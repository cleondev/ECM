using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace samples.EcmFileIntegrationSample.Controllers;

public class HomeController : Controller
{
    private readonly EcmFileClient _client;
    private readonly EcmIntegrationOptions _options;
    private readonly ILogger<HomeController> _logger;
    private readonly EcmAccessTokenProvider _accessTokenProvider;

    public HomeController(
        EcmFileClient client,
        IOptions<EcmIntegrationOptions> options,
        ILogger<HomeController> logger,
        EcmAccessTokenProvider accessTokenProvider)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
        _accessTokenProvider = accessTokenProvider;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var form = BuildDefaultUploadForm();
        var viewModel = await BuildPageViewModelAsync(form, cancellationToken: cancellationToken);
        return View(viewModel);
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
            var viewModel = await BuildPageViewModelAsync(form, cancellationToken: cancellationToken);
            return View("Index", viewModel);
        }

        var profile = await _client.GetCurrentUserProfileAsync(cancellationToken);
        if (profile is null)
        {
            var viewModel = await BuildPageViewModelAsync(
                form,
                cancellationToken: cancellationToken,
                error: "Không lấy được thông tin người dùng từ ECM. Kiểm tra AccessToken trong cấu hình."
            );
            return View("Index", viewModel);
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
                var viewModel = await BuildPageViewModelAsync(
                    form,
                    cancellationToken: cancellationToken,
                    error: "ECM không trả về thông tin tài liệu sau khi upload."
                );
                return View("Index", viewModel);
            }

            var downloadUri = document.LatestVersion is { } version
                ? await _client.GetDownloadUriAsync(version.Id, cancellationToken)
                : null;

            var viewModel = await BuildPageViewModelAsync(
                new UploadFormModel
                {
                    DocType = form.DocType,
                    Status = form.Status,
                    Sensitivity = form.Sensitivity,
                    Title = form.Title,
                },
                result: new UploadResultModel
                {
                    Document = document,
                    DownloadUri = downloadUri,
                    Profile = profile,
                },
                cancellationToken: cancellationToken
            );
            return View("Index", viewModel);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Upload thất bại.");
            var viewModel = await BuildPageViewModelAsync(
                form,
                cancellationToken: cancellationToken,
                error: "Upload thất bại. Kiểm tra log để biết thêm chi tiết."
            );
            return View("Index", viewModel);
        }
        finally
        {
            TryDeleteFile(tempFilePath);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ListDocuments(DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                cancellationToken: cancellationToken
            );
            return View("Index", invalidModel);
        }

        var viewModel = await BuildPageViewModelAsync(
            BuildDefaultUploadForm(),
            documentQuery: documentQuery,
            cancellationToken: cancellationToken,
            documentMessage: "Đã tải danh sách tài liệu theo bộ lọc."
        );

        return View("Index", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTag(TagCreateForm form, DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        var namespaceId = ParseGuidOrNull(form.NamespaceId, nameof(form.NamespaceId));
        var parentId = ParseGuidOrNull(form.ParentId, nameof(form.ParentId));

        if (!ModelState.IsValid)
        {
            var invalidModel = await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                tagCreate: form,
                cancellationToken: cancellationToken
            );
            return View("Index", invalidModel);
        }

        var request = new TagCreateRequest(namespaceId, parentId, form.Name, form.SortOrder, form.Color, form.IconKey, null);
        await _client.CreateTagAsync(request, cancellationToken);

        var viewModel = await BuildPageViewModelAsync(
            BuildDefaultUploadForm(),
            documentQuery: documentQuery,
            tagMessage: "Đã tạo tag mới.",
            cancellationToken: cancellationToken,
            tagCreate: new TagCreateForm()
        );

        return View("Index", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTag(TagUpdateForm form, DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        var tagId = ParseRequiredGuid(form.TagId, nameof(form.TagId));
        var namespaceId = ParseRequiredGuid(form.NamespaceId, nameof(form.NamespaceId));
        var parentId = ParseGuidOrNull(form.ParentId, nameof(form.ParentId));

        if (!ModelState.IsValid || tagId is null || namespaceId is null)
        {
            var invalidModel = await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                tagUpdate: form,
                cancellationToken: cancellationToken
            );
            return View("Index", invalidModel);
        }

        var request = new TagUpdateRequest(namespaceId.Value, parentId, form.Name, form.SortOrder, form.Color, form.IconKey, form.IsActive, null);
        var updated = await _client.UpdateTagAsync(tagId.Value, request, cancellationToken);

        var message = updated is null ? "Không tìm thấy tag cần cập nhật." : "Đã cập nhật tag.";

        var viewModel = await BuildPageViewModelAsync(
            BuildDefaultUploadForm(),
            documentQuery: documentQuery,
            tagMessage: message,
            cancellationToken: cancellationToken,
            tagUpdate: new TagUpdateForm()
        );

        return View("Index", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTag(TagDeleteForm form, DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        var tagId = ParseRequiredGuid(form.TagId, nameof(form.TagId));

        if (!ModelState.IsValid || tagId is null)
        {
            var invalidModel = await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                tagDelete: form,
                cancellationToken: cancellationToken
            );
            return View("Index", invalidModel);
        }

        var deleted = await _client.DeleteTagAsync(tagId.Value, cancellationToken);

        var viewModel = await BuildPageViewModelAsync(
            BuildDefaultUploadForm(),
            documentQuery: documentQuery,
            tagMessage: deleted ? "Đã xoá tag." : "Không tìm thấy hoặc không xoá được tag.",
            cancellationToken: cancellationToken,
            tagDelete: new TagDeleteForm()
        );

        return View("Index", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDocument(DocumentUpdateForm form, DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        var documentId = ParseRequiredGuid(form.DocumentId, nameof(form.DocumentId));
        var groupId = form.UpdateGroup ? ParseGuidOrNull(form.GroupId, nameof(form.GroupId)) : null;

        if (!ModelState.IsValid || documentId is null)
        {
            var invalidModel = await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                documentUpdate: form,
                cancellationToken: cancellationToken
            );
            return View("Index", invalidModel);
        }

        var request = new DocumentUpdateRequest
        {
            Title = form.Title,
            Status = form.Status,
            Sensitivity = form.Sensitivity,
            GroupId = groupId,
            HasGroupId = form.UpdateGroup,
        };

        var updated = await _client.UpdateDocumentAsync(documentId.Value, request, cancellationToken);
        var message = updated is null
            ? "Không thể cập nhật document (không tồn tại hoặc không đủ quyền)."
            : "Đã cập nhật document.";

        var viewModel = await BuildPageViewModelAsync(
            BuildDefaultUploadForm(),
            documentQuery: documentQuery,
            documentMessage: message,
            cancellationToken: cancellationToken,
            documentUpdate: new DocumentUpdateForm()
        );

        return View("Index", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(DocumentDeleteForm form, DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        var documentId = ParseRequiredGuid(form.DocumentId, nameof(form.DocumentId));

        if (!ModelState.IsValid || documentId is null)
        {
            var invalidModel = await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                documentDelete: form,
                cancellationToken: cancellationToken
            );
            return View("Index", invalidModel);
        }

        var deleted = await _client.DeleteDocumentAsync(documentId.Value, cancellationToken);
        var message = deleted
            ? "Đã xoá document."
            : "Không tìm thấy document hoặc không đủ quyền xoá.";

        var viewModel = await BuildPageViewModelAsync(
            BuildDefaultUploadForm(),
            documentQuery: documentQuery,
            documentMessage: message,
            cancellationToken: cancellationToken,
            documentDelete: new DocumentDeleteForm()
        );

        return View("Index", viewModel);
    }

    private UploadFormModel BuildDefaultUploadForm() => new()
    {
        DocType = _options.DocType,
        Status = _options.Status,
        Sensitivity = _options.Sensitivity,
        Title = _options.Title,
    };

    private async Task<UploadPageViewModel> BuildPageViewModelAsync(
        UploadFormModel form,
        DocumentQueryForm? documentQuery = null,
        string? error = null,
        string? tagMessage = null,
        string? documentMessage = null,
        TagCreateForm? tagCreate = null,
        TagUpdateForm? tagUpdate = null,
        TagDeleteForm? tagDelete = null,
        DocumentUpdateForm? documentUpdate = null,
        DocumentDeleteForm? documentDelete = null,
        UploadResultModel? result = null,
        IReadOnlyCollection<TagLabelDto>? tags = null,
        DocumentListResult? documentList = null,
        CancellationToken cancellationToken = default)
    {
        documentQuery ??= new DocumentQueryForm();

        if (tags is null || documentList is null)
        {
            var reference = await LoadReferenceDataAsync(documentQuery, cancellationToken);
            tags ??= reference.Tags;
            documentList ??= reference.Documents;
        }

        return new UploadPageViewModel
        {
            BaseUrl = _options.BaseUrl,
            HasAccessToken = _accessTokenProvider.HasConfiguredAccess,
            UsingOnBehalfAuthentication = _accessTokenProvider.UsingOnBehalfAuthentication,
            OnBehalfUserEmail = _options.OnBehalf.UserEmail,
            OnBehalfUserId = _options.OnBehalf.UserId,
            Form = form,
            Result = result,
            Error = error,
            Tags = tags,
            TagMessage = tagMessage,
            TagCreate = tagCreate ?? new TagCreateForm(),
            TagUpdate = tagUpdate ?? new TagUpdateForm(),
            TagDelete = tagDelete ?? new TagDeleteForm(),
            DocumentQuery = documentQuery,
            DocumentList = documentList,
            DocumentMessage = documentMessage,
            DocumentUpdate = documentUpdate ?? new DocumentUpdateForm(),
            DocumentDelete = documentDelete ?? new DocumentDeleteForm(),
        };
    }

    private async Task<(IReadOnlyCollection<TagLabelDto> Tags, DocumentListResult? Documents)> LoadReferenceDataAsync(
        DocumentQueryForm documentQuery,
        CancellationToken cancellationToken)
    {
        var tags = Array.Empty<TagLabelDto>();
        DocumentListResult? documents = null;

        try
        {
            tags = await _client.ListTagsAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Không thể tải danh sách tag.");
        }

        try
        {
            var query = BuildDocumentListQuery(documentQuery);
            documents = await _client.ListDocumentsAsync(query, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Không thể tải danh sách document.");
        }

        return (tags, documents);
    }

    private static DocumentListQuery BuildDocumentListQuery(DocumentQueryForm documentQuery) => new(
        documentQuery.Query,
        documentQuery.DocType,
        documentQuery.Status,
        documentQuery.Sensitivity,
        null,
        null,
        documentQuery.Page,
        documentQuery.PageSize);

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

    private Guid? ParseRequiredGuid(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ModelState.AddModelError(fieldName, "Giá trị bắt buộc (GUID).");
            return null;
        }

        return ParseGuidOrNull(value, fieldName);
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
