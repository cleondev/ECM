using System;

namespace ECM.Document.Api.Documents;

public sealed class DocumentUploadDefaultsOptions
{
    public const string SectionName = "DocumentUploadDefaults";

    public string DocType { get; set; } = "general";

    public string Status { get; set; } = "draft";

    public string Sensitivity { get; set; } = "Internal";

    public string? Department { get; set; }

    public Guid? OwnerId { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? DocumentTypeId { get; set; }
}
