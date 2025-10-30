using System.ComponentModel.DataAnnotations;

namespace ECM.Ocr.Infrastructure.DotOcr;

public sealed class DotOcrOptions
{
    public const string SectionName = "Ocr:Dot";

    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    public string ChatCompletionsEndpoint { get; set; } = "v1/chat/completions";

    public string Model { get; set; } = "dotsocr-model";

    public double Temperature { get; set; } = 0;

    public int MaxTokens { get; set; } = 2048;

    public string Instruction { get; set; } = "Please output the layout information from the PDF image, including each layout element's bbox, its category, and the corresponding text content within the bbox.\n\n1. Bbox format: [x1, y1, x2, y2]\n\n2. Layout Categories: ['Caption', 'Footnote', 'Formula', 'List-item', 'Page-footer', 'Page-header', 'Picture', 'Section-header', 'Table', 'Text', 'Title'].\n\n3. Text Extraction & Formatting Rules:\n    - Picture: omit text.\n    - Formula: use LaTeX.\n    - Table: use HTML.\n    - All others: use Markdown.\n\n4. Output original text only, no translation.\n5. Return a single JSON object.";

    public string SampleResultEndpointTemplate { get; set; } = "api/samples/{sampleId}/results";

    public string BoxingResultEndpointTemplate { get; set; } = "api/samples/{sampleId}/boxings/{boxingId}/results";

    public string BoxesEndpointTemplate { get; set; } = "api/samples/{sampleId}/boxes";

    public string BoxValueEndpointTemplate { get; set; } = "api/samples/{sampleId}/boxes/{boxId}";

    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 100;
}
