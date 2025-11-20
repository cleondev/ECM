using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace samples.EcmFileIntegrationSample.Controllers;

public class HomeController : Controller
{
    private readonly EcmFileClient _client;
    private readonly EcmIntegrationOptions _options;
    private readonly ILogger<HomeController> _logger;

    public HomeController(EcmFileClient client, IOptions<EcmIntegrationOptions> options, ILogger<HomeController> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var form = new UploadFormModel
        {
            DocType = _options.DocType,
            Status = _options.Status,
            Sensitivity = _options.Sensitivity,
            Title = _options.Title,
        };

        return View(new UploadPageViewModel
        {
            BaseUrl = _options.BaseUrl,
            HasAccessToken = !string.IsNullOrWhiteSpace(_options.AccessToken),
            Form = form,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload([Bind(Prefix = "Form")] UploadFormModel form, CancellationToken cancellationToken)
    {
        var documentTypeId = ParseGuidOrNull(form.DocumentTypeId, nameof(form.DocumentTypeId));
        var ownerId = ParseGuidOrNull(form.OwnerId, nameof(form.OwnerId));
        var createdBy = ParseGuidOrNull(form.CreatedBy, nameof(form.CreatedBy));

        if (form.File is null || form.File.Length == 0)
        {
            ModelState.AddModelError(nameof(form.File), "File bắt buộc và không được rỗng.");
        }

        if (!ModelState.IsValid)
        {
            return View("Index", BuildViewModel(form));
        }

        var profile = await _client.GetCurrentUserProfileAsync(cancellationToken);
        if (profile is null)
        {
            return View("Index", BuildViewModel(form, "Không lấy được thông tin người dùng từ ECM. Kiểm tra AccessToken trong cấu hình."));
        }

        ownerId ??= profile.Id;
        createdBy ??= profile.Id;

        var tempFilePath = Path.GetTempFileName();

        try
        {
            await using (var stream = System.IO.File.Create(tempFilePath))
            {
                await form.File!.CopyToAsync(stream, cancellationToken);
            }

            var uploadRequest = new DocumentUploadRequest(
                ownerId.Value,
                createdBy.Value,
                string.IsNullOrWhiteSpace(form.DocType) ? _options.DocType : form.DocType,
                string.IsNullOrWhiteSpace(form.Status) ? _options.Status : form.Status,
                string.IsNullOrWhiteSpace(form.Sensitivity) ? _options.Sensitivity : form.Sensitivity,
                tempFilePath)
            {
                DocumentTypeId = documentTypeId,
                Title = string.IsNullOrWhiteSpace(form.Title) ? form.File!.FileName : form.Title,
                ContentType = string.IsNullOrWhiteSpace(form.File!.ContentType) ? null : form.File.ContentType,
            };

            var document = await _client.UploadDocumentAsync(uploadRequest, cancellationToken);
            if (document is null)
            {
                return View("Index", BuildViewModel(form, "ECM không trả về thông tin tài liệu sau khi upload."));
            }

            var downloadUri = document.LatestVersion is { } version
                ? await _client.GetDownloadUriAsync(version.Id, cancellationToken)
                : null;

            return View("Index", new UploadPageViewModel
            {
                BaseUrl = _options.BaseUrl,
                HasAccessToken = !string.IsNullOrWhiteSpace(_options.AccessToken),
                Form = new UploadFormModel
                {
                    DocType = form.DocType,
                    Status = form.Status,
                    Sensitivity = form.Sensitivity,
                    Title = form.Title,
                },
                Result = new UploadResultModel
                {
                    Document = document,
                    DownloadUri = downloadUri,
                    Profile = profile,
                },
            });
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Upload thất bại.");
            return View("Index", BuildViewModel(form, "Upload thất bại. Kiểm tra log để biết thêm chi tiết."));
        }
        finally
        {
            TryDeleteFile(tempFilePath);
        }
    }

    private UploadPageViewModel BuildViewModel(UploadFormModel form, string? error = null) => new()
    {
        BaseUrl = _options.BaseUrl,
        HasAccessToken = !string.IsNullOrWhiteSpace(_options.AccessToken),
        Form = form,
        Error = error,
    };

    private Guid? ParseGuidOrNull(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (Guid.TryParse(value, out var parsed))
        {
            return parsed;
        }

        ModelState.AddModelError(fieldName, "Giá trị không hợp lệ (yêu cầu GUID).");
        return null;
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }
        }
        catch (IOException exception)
        {
            _logger.LogWarning(exception, "Không thể xóa file tạm {Path} sau khi upload.", path);
        }
    }
}
