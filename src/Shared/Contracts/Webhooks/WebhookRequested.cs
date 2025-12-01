namespace Shared.Contracts.Webhooks;

public sealed record WebhookRequested
{
    // Id logic của yêu cầu webhook (dùng làm idempotency key chính)
    public string RequestId { get; init; } = default!;

    // Tên endpoint cấu hình sẵn (ví dụ: "UploadCallback_PGB")
    public string EndpointKey { get; init; } = default!;

    // Payload JSON đã chuẩn hoá (string)
    public string PayloadJson { get; init; } = default!;

    // Optional: correlation
    public string? CorrelationId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
