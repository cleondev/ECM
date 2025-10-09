namespace AppGateway.Contracts.Signatures;

public sealed record SignatureReceiptDto(Guid RequestId, Guid DocumentId, string Status, DateTimeOffset RequestedAtUtc);
