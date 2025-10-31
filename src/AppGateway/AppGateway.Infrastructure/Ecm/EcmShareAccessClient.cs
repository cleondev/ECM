using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;

namespace AppGateway.Infrastructure.Ecm;

internal sealed class EcmShareAccessClient(HttpClient httpClient) : IEcmShareAccessClient
{
    private readonly HttpClient _httpClient = httpClient;

    public Task<HttpResponseMessage> GetInterstitialAsync(
        string code,
        string? password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var path = BuildPath($"s/{Uri.EscapeDataString(code)}", password);
        var request = CreateRequest(HttpMethod.Get, path);

        return _httpClient.SendAsync(request, cancellationToken);
    }

    public Task<HttpResponseMessage> VerifyPasswordAsync(
        string code,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var request = CreateRequest(HttpMethod.Post, $"s/{Uri.EscapeDataString(code)}/password");
        request.Content = JsonContent.Create(new { password });

        return _httpClient.SendAsync(request, cancellationToken);
    }

    public Task<HttpResponseMessage> CreatePresignedUrlAsync(
        string code,
        string? password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var request = CreateRequest(HttpMethod.Post, $"s/{Uri.EscapeDataString(code)}/presign");

        if (!string.IsNullOrWhiteSpace(password))
        {
            request.Content = JsonContent.Create(new { password });
        }
        else
        {
            request.Content = JsonContent.Create(new { });
        }

        return _httpClient.SendAsync(request, cancellationToken);
    }

    public Task<HttpResponseMessage> RedirectToDownloadAsync(
        string code,
        string? password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var path = BuildPath($"s/{Uri.EscapeDataString(code)}/download", password);
        var request = CreateRequest(HttpMethod.Get, path);

        return _httpClient.SendAsync(request, cancellationToken);
    }

    private static string BuildPath(string path, string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return path;
        }

        return QueryHelpers.AddQueryString(path, "password", password);
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }
}
