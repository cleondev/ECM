using System;
using System.Threading;
using System.Threading.Tasks;

namespace ECM.Ocr.Application.Abstractions;

public interface IDocumentFileLinkService
{
    Task<Uri?> GetDownloadLinkAsync(Guid documentId, CancellationToken cancellationToken = default);
}
