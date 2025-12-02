namespace ECM.Document.Application.Shares;

public sealed class ShareLinkOptions
{
    public const string SectionName = "Shares";

    public string? PublicBaseUrl { get; set; }

    public int MinCodeLength { get; set; } = 7;

    public int MaxCodeLength { get; set; } = 10;

    public TimeSpan DefaultPresignLifetime { get; set; } = TimeSpan.FromMinutes(10);
}
