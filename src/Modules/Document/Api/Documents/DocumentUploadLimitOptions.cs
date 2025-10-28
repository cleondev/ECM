using System;

namespace ECM.Document.Api.Documents;

public sealed class DocumentUploadLimitOptions
{
    public const string SectionName = "DocumentUploadLimits";
    private const long OneGigabyte = 1024L * 1024L * 1024L;

    public long MaxRequestBodySize { get; set; } = OneGigabyte;

    public long MultipartBodyLengthLimit { get; set; } = OneGigabyte;

    public void EnsureValid()
    {
        if (MaxRequestBodySize <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MaxRequestBodySize),
                MaxRequestBodySize,
                "Upload limit must be greater than zero.");
        }

        if (MultipartBodyLengthLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(MultipartBodyLengthLimit),
                MultipartBodyLengthLimit,
                "Upload limit must be greater than zero.");
        }
    }
}
