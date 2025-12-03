using Microsoft.Extensions.Options;

using Npgsql;

using NpgsqlTypes;

namespace OutboxDispatcher;

/// <summary>
///     Pulls pending rows from the <c>ops.outbox</c> table and forwards them to the message bus.
///     The processor is responsible for ensuring idempotency, retry semantics, and dead-letter handling.
/// </summary>
internal sealed class OutboxMessageProcessor
{
    // The SKIP LOCKED hint allows multiple dispatcher instances to cooperate without stepping on each other.
    private const string FetchNextSql = """
        SELECT id, aggregate, aggregate_id, type, payload, occurred_at
        FROM ops.outbox
        WHERE processed_at IS NULL
        ORDER BY occurred_at
        FOR UPDATE SKIP LOCKED
        LIMIT 1;
        """;

    private readonly ILogger<OutboxMessageProcessor> _logger;
    private readonly NpgsqlDataSource _dataSource;
    private readonly OutboxDispatcherOptions _options;
    private readonly OutboxMessageDispatcher _dispatcher;

    public OutboxMessageProcessor(
        NpgsqlDataSource dataSource,
        OutboxMessageDispatcher dispatcher,
        IOptions<OutboxDispatcherOptions> options,
        ILogger<OutboxMessageProcessor> logger)
    {
        _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

        if (_options.BatchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Batch size must be greater than zero.");
        }

        if (_options.MaxRetryAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Retry attempts must be greater than zero.");
        }
    }

    /// <summary>
    ///     Reads a batch of pending messages, dispatches them, and updates the outbox state.
    ///     The method returns the number of messages successfully marked as processed.
    /// </summary>
    public async Task<int> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);
        var processedCount = 0;

        for (var i = 0; i < _options.BatchSize && !cancellationToken.IsCancellationRequested; i++)
        {
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            var message = await TryFetchNextMessageAsync(connection, transaction, cancellationToken).ConfigureAwait(false);

            if (message is null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                break;
            }

            // Each message is handled in its own transaction so that long-running dispatch work does not hold locks
            // over the entire batch. Multiple worker instances can therefore scale out without blocking one another.
            var processed = await HandleMessageAsync(connection, transaction, message, cancellationToken).ConfigureAwait(false);
            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            if (processed)
            {
                processedCount++;
            }
        }

        return processedCount;
    }

    private static async Task<PendingOutboxMessage?> TryFetchNextMessageAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = FetchNextSql;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

        if (!await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            return null;
        }

        var id = reader.GetInt64(0);
        var aggregate = reader.GetString(1);
        var aggregateId = reader.GetGuid(2);
        var type = reader.GetString(3);
        var payload = reader.GetString(4);
        var occurredAt = await reader.GetFieldValueAsync<DateTimeOffset>(5);

        return new PendingOutboxMessage(id, aggregate, aggregateId, type, payload, occurredAt);
    }

    private async Task<bool> HandleMessageAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        PendingOutboxMessage message,
        CancellationToken cancellationToken)
    {
        var attempt = 0;
        Exception? lastError = null;

        while (attempt < _options.MaxRetryAttempts && !cancellationToken.IsCancellationRequested)
        {
            attempt++;

            try
            {
                await _dispatcher.DispatchAsync(message, cancellationToken).ConfigureAwait(false);
                await MarkAsProcessedAsync(connection, transaction, message.Id, cancellationToken).ConfigureAwait(false);
                _logger.LogInformation(
                    "Dispatched outbox message {MessageId} of type {MessageType} on attempt {Attempt}.",
                    message.Id,
                    message.Type,
                    attempt);
                return true;
            }
            catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
            {
                lastError = exception;
                _logger.LogWarning(
                    exception,
                    "Failed to dispatch outbox message {MessageId} on attempt {Attempt}.",
                    message.Id,
                    attempt);

                if (attempt >= _options.MaxRetryAttempts)
                {
                    break;
                }

                var delay = CalculateRetryDelay(attempt);
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        await MoveToDeadLetterAsync(connection, transaction, message, lastError, cancellationToken).ConfigureAwait(false);
        await MarkAsProcessedAsync(connection, transaction, message.Id, cancellationToken).ConfigureAwait(false);
        return false;
    }

    private static async Task MarkAsProcessedAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        long messageId,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "UPDATE ops.outbox SET processed_at = now() WHERE id = @id;";

        var identifier = command.Parameters.Add("id", NpgsqlDbType.Bigint);
        identifier.Value = messageId;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task MoveToDeadLetterAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        PendingOutboxMessage message,
        Exception? exception,
        CancellationToken cancellationToken)
    {
        var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            INSERT INTO ops.outbox_deadletter (id, type, payload, error, failed_at)
            VALUES (@id, @type, @payload, @error, now())
            ON CONFLICT (id) DO UPDATE
            SET type = excluded.type,
                payload = excluded.payload,
                error = excluded.error,
                failed_at = excluded.failed_at;
            """;

        command.Parameters.Add("id", NpgsqlDbType.Bigint).Value = message.Id;
        command.Parameters.Add("type", NpgsqlDbType.Text).Value = message.Type;
        command.Parameters.Add("payload", NpgsqlDbType.Jsonb).Value = message.Payload;

        var errorMessage = exception?.ToString() ?? "Unknown error";
        command.Parameters.Add("error", NpgsqlDbType.Text).Value = errorMessage;

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogError(
            exception,
            "Moved outbox message {MessageId} to dead-letter queue after {MaxAttempts} attempts.",
            message.Id,
            _options.MaxRetryAttempts);
    }

    private TimeSpan CalculateRetryDelay(int attempt)
    {
        if (_options.InitialRetryDelay <= TimeSpan.Zero)
        {
            return TimeSpan.Zero;
        }

        var exponent = attempt - 1;
        var multiplier = Math.Pow(2, exponent);
        var rawDelay = TimeSpan.FromMilliseconds(_options.InitialRetryDelay.TotalMilliseconds * multiplier);
        return rawDelay <= _options.MaxRetryDelay ? rawDelay : _options.MaxRetryDelay;
    }

}
