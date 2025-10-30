using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ECM.Document.Application.Documents.AccessControl;

public interface IEffectiveAclFlatWriter
{
    Task UpsertAsync(EffectiveAclFlatWriteEntry entry, CancellationToken cancellationToken = default);

    Task UpsertAsync(IEnumerable<EffectiveAclFlatWriteEntry> entries, CancellationToken cancellationToken = default);
}
