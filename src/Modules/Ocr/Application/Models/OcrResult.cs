using System.Text.Json;

namespace ECM.Ocr.Application.Models;

public sealed record OcrResult(JsonElement Data)
{
    public static OcrResult Empty { get; } = new(JsonDocument.Parse("null").RootElement.Clone());
}
