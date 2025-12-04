using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AppGateway.Infrastructure.Ecm;

public sealed class CreateDocumentUpload
{
    public string Title { get; init; } = string.Empty;

    public string DocType { get; init; } = string.Empty;

    public string Status { get; init; } = string.Empty;

    public Guid? OwnerId { get; init; }

    public Guid? CreatedBy { get; init; }

    public Guid? GroupId { get; init; }

    public string? Sensitivity { get; init; }

    public Guid? DocumentTypeId { get; init; }

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = "application/octet-stream";

    public long FileSize { get; init; }

    public Func<CancellationToken, Task<Stream>> OpenReadStream { get; init; } = default!;
}
