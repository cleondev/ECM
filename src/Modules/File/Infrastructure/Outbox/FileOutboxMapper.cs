using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ECM.BuildingBlocks.Domain.Events;
using ECM.File.Domain.Files.Events;
using ECM.Operations.Infrastructure.Outbox;
using ECM.Operations.Infrastructure.Persistence;

using Shared.Contracts.Files;
using Shared.Contracts.Messaging;

namespace ECM.File.Infrastructure.Outbox;

internal static class FileOutboxMapper
{
    public static IEnumerable<OutboxMessage> ToOutboxMessages(IEnumerable<IDomainEvent> domainEvents)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);
        return domainEvents.Select(ToOutboxMessage).OfType<OutboxMessage>();
    }

    private static OutboxMessage? ToOutboxMessage(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            StoredFileUploadedDomainEvent uploaded => Map(uploaded),
            _ => null
        };
    }

    private static OutboxMessage Map(StoredFileUploadedDomainEvent domainEvent)
    {
        var contract = new StoredFileUploadedContract(
            domainEvent.StorageKey,
            domainEvent.LegalHold,
            domainEvent.OccurredAtUtc);

        return OutboxMessageFactory.Create(
            aggregate: "file",
            aggregateId: CreateDeterministicGuid(domainEvent.StorageKey),
            type: EventNames.File.Uploaded,
            payload: contract,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static Guid CreateDeterministicGuid(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        var hash = SHA256.HashData(bytes);
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}
