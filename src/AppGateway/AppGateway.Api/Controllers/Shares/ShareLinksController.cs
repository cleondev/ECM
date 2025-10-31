using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using AppGateway.Infrastructure.Ecm;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace AppGateway.Api.Controllers.Shares;

[ApiController]
[Route("api/share-links")]
[AllowAnonymous]
public sealed class ShareLinksController(IEcmShareAccessClient client, ILogger<ShareLinksController> logger) : ControllerBase
{
    private readonly IEcmShareAccessClient _client = client;
    private readonly ILogger<ShareLinksController> _logger = logger;

    [HttpGet("{code}")]
    public async Task<IActionResult> GetAsync(string code, [FromQuery] string? password, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.GetInterstitialAsync(code, password, cancellationToken);
            return new ProxyResult(response);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Failed to fetch share interstitial for code {Code}", code);
            return Problem(
                title: "Không thể tải thông tin chia sẻ từ máy chủ ECM.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpPost("{code}/password")]
    public async Task<IActionResult> PostPasswordAsync(
        string code,
        [FromBody] SharePasswordRequest request,
        CancellationToken cancellationToken)
    {
        request ??= new SharePasswordRequest(null);

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            return ValidationProblem(new()
            {
                [nameof(request.Password)] = ["Password is required."]
            });
        }

        try
        {
            var response = await _client.VerifyPasswordAsync(code, request.Password, cancellationToken);
            return new ProxyResult(response);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Failed to verify share password for code {Code}", code);
            return Problem(
                title: "Không thể xác thực mật khẩu với máy chủ ECM.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpPost("{code}/presign")]
    public async Task<IActionResult> PostPresignAsync(
        string code,
        [FromBody] SharePasswordRequest request,
        CancellationToken cancellationToken)
    {
        request ??= new SharePasswordRequest(null);

        try
        {
            var response = await _client.CreatePresignedUrlAsync(code, request.Password, cancellationToken);
            return new ProxyResult(response);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Failed to create presigned URL for share {Code}", code);
            return Problem(
                title: "Không thể tạo liên kết tải xuống từ máy chủ ECM.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    [HttpGet("{code}/download")]
    public async Task<IActionResult> GetDownloadAsync(
        string code,
        [FromQuery] string? password,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _client.RedirectToDownloadAsync(code, password, cancellationToken);
            return new ProxyResult(response);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Failed to redirect share download for code {Code}", code);
            return Problem(
                title: "Không thể chuyển hướng tải xuống từ máy chủ ECM.",
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    public sealed record SharePasswordRequest(string? Password);

    private sealed class ProxyResult(HttpResponseMessage response) : IActionResult
    {
        private readonly HttpResponseMessage _response = response;

        public async Task ExecuteResultAsync(ActionContext context)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            await using var _ = new ResponseDisposer(_response);

            var httpResponse = context.HttpContext.Response;
            httpResponse.StatusCode = (int)_response.StatusCode;

            foreach (var header in _response.Headers)
            {
                if (string.Equals(header.Key, "Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                httpResponse.Headers[header.Key] = new StringValues(header.Value.ToArray());
            }

            if (_response.Content is null)
            {
                return;
            }

            foreach (var header in _response.Content.Headers)
            {
                if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                {
                    httpResponse.ContentType = string.Join(",", header.Value);
                    continue;
                }

                if (string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                {
                    if (long.TryParse(header.Value.FirstOrDefault(), out var length))
                    {
                        httpResponse.ContentLength = length;
                    }

                    continue;
                }

                httpResponse.Headers[header.Key] = new StringValues(header.Value.ToArray());
            }

            await _response.Content.CopyToAsync(httpResponse.Body);
        }

        private sealed class ResponseDisposer(HttpResponseMessage response) : IAsyncDisposable
        {
            private readonly HttpResponseMessage _response = response;

            public ValueTask DisposeAsync()
            {
                _response.Dispose();
                return ValueTask.CompletedTask;
            }
        }
    }
}
