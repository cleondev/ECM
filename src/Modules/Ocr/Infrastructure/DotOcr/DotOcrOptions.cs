using System.ComponentModel.DataAnnotations;

namespace ECM.Ocr.Infrastructure.DotOcr;

public sealed class DotOcrOptions
{
    public const string SectionName = "Ocr:Dot";

    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    public string StartEndpoint { get; set; } = "api/samples";

    public string SampleResultEndpointTemplate { get; set; } = "api/samples/{sampleId}/results";

    public string BoxingResultEndpointTemplate { get; set; } = "api/samples/{sampleId}/boxings/{boxingId}/results";

    public string BoxesEndpointTemplate { get; set; } = "api/samples/{sampleId}/boxes";

    public string BoxValueEndpointTemplate { get; set; } = "api/samples/{sampleId}/boxes/{boxId}";

    public string? ApiKey { get; set; }

    public int TimeoutSeconds { get; set; } = 100;
}
