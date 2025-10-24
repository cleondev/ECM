using System.Text.Json;

namespace ECM.Ocr.Application.Models;

public sealed record OcrBoxesResult(JsonElement Data)
{
    public static OcrBoxesResult Empty { get; } = new(JsonDocument.Parse("[]").RootElement.Clone());
}
