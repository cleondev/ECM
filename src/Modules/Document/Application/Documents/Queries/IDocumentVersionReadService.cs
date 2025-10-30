using System;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Application.Documents.Summaries;

namespace ECM.Document.Application.Documents.Queries;

public interface IDocumentVersionReadService
{
    Task<DocumentVersionResult?> GetByIdAsync(Guid versionId, CancellationToken cancellationToken = default);
}
