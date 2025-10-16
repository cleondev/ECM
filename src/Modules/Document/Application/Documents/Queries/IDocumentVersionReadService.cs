using System;
using System.Threading;
using System.Threading.Tasks;

namespace ECM.Document.Application.Documents.Queries;

public interface IDocumentVersionReadService
{
    Task<DocumentVersionReadModel?> GetByIdAsync(Guid versionId, CancellationToken cancellationToken = default);
}
