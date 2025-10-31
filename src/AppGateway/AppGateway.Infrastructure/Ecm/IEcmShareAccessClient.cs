using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace AppGateway.Infrastructure.Ecm;

public interface IEcmShareAccessClient
{
    Task<HttpResponseMessage> GetInterstitialAsync(
        string code,
        string? password,
        CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> VerifyPasswordAsync(
        string code,
        string password,
        CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> CreatePresignedUrlAsync(
        string code,
        string? password,
        CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> RedirectToDownloadAsync(
        string code,
        string? password,
        CancellationToken cancellationToken = default);
}
