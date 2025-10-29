using System;
using System.Collections.Generic;

namespace ECM.Document.Api.Documents.Options;

public sealed class DocumentUploadDefaultsOptions
{
    public const string SectionName = "DocumentUploadDefaults";

    public string DocType { get; set; } = "general";

    public string Status { get; set; } = "draft";

    public string Sensitivity { get; set; } = "Internal";

    public Guid? GroupId { get; set; }

    public IReadOnlyCollection<Guid> GroupIds { get; set; } = Array.Empty<Guid>();

    public Guid? OwnerId { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? DocumentTypeId { get; set; }
}
