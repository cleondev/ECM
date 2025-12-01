using System;

namespace OutboxDispatcher;

/// <summary>
///     Configuration knobs controlling how the outbox dispatcher polls the database and retries failures.
/// </summary>
public sealed class OutboxDispatcherOptions
{
    /// <summary>
    ///     Gets or sets the delay between polling cycles when the dispatcher does not find any work.
    ///     A sensible default keeps the worker responsive without constantly hammering the database.
    /// </summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    ///     Gets or sets the maximum number of messages fetched in a single batch.
    ///     Larger batch sizes reduce round-trips but also increase the time a transaction keeps row locks.
    /// </summary>
    public int BatchSize { get; set; } = 100;

    /// <summary>
    ///     Gets or sets the number of attempts performed before a message is sent to the dead-letter queue.
    ///     The value must be at least one to guarantee forward progress.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    ///     Gets or sets the initial delay applied between retry attempts.
    ///     The dispatcher uses exponential back-off capped by <see cref="MaxRetryDelay"/>.
    /// </summary>
    public TimeSpan InitialRetryDelay { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    ///     Gets or sets the maximum delay the dispatcher is allowed to wait between retries.
    ///     Keeping the value bounded ensures that row locks are not held for too long.
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(3);
}
