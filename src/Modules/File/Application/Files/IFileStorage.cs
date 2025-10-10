using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ECM.File.Application.Files;

public interface IFileStorage
{
    Task UploadAsync(string storageKey, Stream content, string contentType, CancellationToken cancellationToken = default);
}
