namespace ECM.Ocr.Application.Models;

public sealed record StartOcrResult(string? SampleId)
{
    public static StartOcrResult Empty { get; } = new(null);
}
