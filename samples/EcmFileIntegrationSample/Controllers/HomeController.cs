using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ecm.Sdk.Models.Documents;
using Ecm.Sdk.Models.Tags;
using Ecm.Sdk.Configuration;
using Ecm.Sdk.Clients;
using samples.EcmFileIntegrationSample;

namespace EcmFileIntegrationSample.Controllers;

public class HomeController(
    EcmFileClient client,
    IOptionsSnapshot<EcmIntegrationOptions> options,
    ILogger<HomeController> logger,
    EcmUserSelection userSelection) : Controller
{
    private readonly EcmFileClient _client = client;
    private readonly IOptionsSnapshot<EcmIntegrationOptions> _optionsSnapshot = options;
    private readonly ILogger<HomeController> _logger = logger;
    private readonly EcmUserSelection _userSelection = userSelection;

    private EcmIntegrationOptions Options => _optionsSnapshot.Value;

    // ----------------------------------------------------
    // Index
    // ----------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ApplyUserSelection(Options.OnBehalfUserEmail, Options.OnBehalfUserEmail);
        var form = BuildDefaultUploadForm();
        return View(await BuildPageViewModelAsync(form, cancellationToken: cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SwitchUser(string userEmail, CancellationToken cancellationToken)
    {
        ApplyUserSelection(userEmail, null);

        return View(
            "Index",
            await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                cancellationToken: cancellationToken));
    }

    // ----------------------------------------------------
    // Upload
    // ----------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Upload([Bind(Prefix = "Form")] UploadFormModel form, CancellationToken cancellationToken)
    {
        var selectedUser = ApplyUserSelection(form.UserEmail, null);
        var documentTypeId = ParseGuidOrNull(form.DocumentTypeId, nameof(form.DocumentTypeId));
        var ownerId = ParseGuidOrNull(form.OwnerId, nameof(form.OwnerId));
        var createdBy = ParseGuidOrNull(form.CreatedBy, nameof(form.CreatedBy));
        var selectedTagIds = ParseGuidList(form.SelectedTagIds, nameof(form.SelectedTagIds));

        if (form.File is null || form.File.Length == 0)
        {
            ModelState.AddModelError(nameof(form.File), "File bắt buộc và không được rỗng.");
        }

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildPageViewModelAsync(form, cancellationToken: cancellationToken));
        }

        var profile = await _client.GetCurrentUserProfileAsync(cancellationToken);
        if (profile is null)
        {
                return View("Index", await BuildPageViewModelAsync(
                    form,
                    cancellationToken: cancellationToken,
                    error: "Không lấy được thông tin người dùng từ ECM. Kiểm tra cấu hình ApiKey/SSO."
                ));
            }

        ownerId ??= profile.Id;
        createdBy ??= profile.Id;

        var tempFilePath = Path.GetRandomFileName();

        try
        {
            await using (var stream = System.IO.File.Create(tempFilePath))
            {
                await form.File!.CopyToAsync(stream, cancellationToken);
            }

            var uploadRequest = new DocumentUploadRequest(
                ownerId.Value,
                createdBy.Value,
                string.IsNullOrWhiteSpace(form.DocType) ? Options.DocType : form.DocType,
                string.IsNullOrWhiteSpace(form.Status) ? Options.Status : form.Status,
                string.IsNullOrWhiteSpace(form.Sensitivity) ? Options.Sensitivity : form.Sensitivity,
                tempFilePath)
            {
                DocumentTypeId = documentTypeId,
                Title = string.IsNullOrWhiteSpace(form.Title) ? form.File!.FileName : form.Title,
                ContentType = string.IsNullOrWhiteSpace(form.File!.ContentType) ? null : form.File.ContentType
            };

            var document = await _client.UploadDocumentAsync(uploadRequest, cancellationToken);
            if (document is null)
            {
                return View("Index", await BuildPageViewModelAsync(
                    form,
                    cancellationToken: cancellationToken,
                    error: "ECM không trả về thông tin tài liệu sau khi upload."
                ));
            }

            var downloadUri = document.LatestVersion is { } version
                ? await _client.GetDownloadUriAsync(version.Id, cancellationToken)
                : null;

            IReadOnlyCollection<TagLabelDto> appliedTags = [];
            IReadOnlyCollection<TagLabelDto> tags = [];

            try
            {
                tags = await _client.ListTagsAsync(cancellationToken);
                appliedTags = await ApplyTagsAsync(document.Id, selectedTagIds, profile.Id, tags, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Không thể gán tag cho document sau khi upload.");
            }

            return View("Index", await BuildPageViewModelAsync(
                new UploadFormModel
                {
                    DocType = form.DocType,
                    Status = form.Status,
                    Sensitivity = form.Sensitivity,
                    Title = form.Title,
                    UserEmail = form.UserEmail ?? selectedUser.Email,
                    SelectedTagIds = form.SelectedTagIds,
                },
                result: new UploadResultModel
                {
                    Document = document,
                    DownloadUri = downloadUri,
                    Profile = profile,
                    AppliedTags = appliedTags,
                },
                tags: tags,
                cancellationToken: cancellationToken
            ));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Upload thất bại.");
            return View("Index", await BuildPageViewModelAsync(
                form,
                cancellationToken: cancellationToken,
                error: "Upload thất bại. Kiểm tra log để biết thêm chi tiết."
            ));
        }
        finally
        {
            TryDeleteFile(tempFilePath);
        }
    }

    // ----------------------------------------------------
    // List Documents
    // ----------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ListDocuments(DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        ApplyUserSelection(documentQuery.UserEmail, null);
        if (!ModelState.IsValid)
        {
            return View("Index", await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                cancellationToken: cancellationToken
            ));
        }

        return View("Index", await BuildPageViewModelAsync(
            BuildDefaultUploadForm(),
            documentQuery: documentQuery,
            cancellationToken: cancellationToken,
            documentMessage: "Đã tải danh sách tài liệu theo bộ lọc."
        ));
    }

    // ----------------------------------------------------
    // Create Tag
    // ----------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTag(TagCreateForm form, DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        ApplyUserSelection(form.UserEmail ?? documentQuery.UserEmail, documentQuery.UserEmail);
        var namespaceId = ParseGuidOrNull(form.NamespaceId, nameof(form.NamespaceId));
        var parentId = ParseGuidOrNull(form.ParentId, nameof(form.ParentId));

        if (!ModelState.IsValid)
        {
            return View("Index", await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                tagCreate: form,
                cancellationToken: cancellationToken
            ));
        }

        var request = new TagCreateRequest(namespaceId, parentId, form.Name, form.SortOrder, form.Color, form.IconKey, null);
        await _client.CreateTagAsync(request, cancellationToken);

        return View("Index", await BuildPageViewModelAsync(
            BuildDefaultUploadForm(),
            documentQuery: documentQuery,
            tagMessage: "Đã tạo tag mới.",
            cancellationToken: cancellationToken,
            tagCreate: new TagCreateForm()
        ));
    }

    // ----------------------------------------------------
    // Update Tag
    // ----------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTag(TagUpdateForm form, DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        ApplyUserSelection(form.UserEmail ?? documentQuery.UserEmail, documentQuery.UserEmail);
        var tagId = ParseRequiredGuid(form.TagId, nameof(form.TagId));
        var namespaceId = ParseRequiredGuid(form.NamespaceId, nameof(form.NamespaceId));
        var parentId = ParseGuidOrNull(form.ParentId, nameof(form.ParentId));

        if (!ModelState.IsValid || tagId is null || namespaceId is null)
        {
            return View("Index", await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                tagUpdate: form,
                cancellationToken: cancellationToken
            ));
        }

        var request = new TagUpdateRequest(namespaceId.Value, parentId, form.Name, form.SortOrder, form.Color, form.IconKey, form.IsActive, null);
        var updated = await _client.UpdateTagAsync(tagId.Value, request, cancellationToken);

        var message = updated is null ? "Không tìm thấy tag cần cập nhật." : "Đã cập nhật tag.";

        return View("Index", await BuildPageViewModelAsync(
            BuildDefaultUploadForm(),
            documentQuery: documentQuery,
            tagMessage: message,
            cancellationToken: cancellationToken,
            tagUpdate: new TagUpdateForm()
        ));
    }

    // ----------------------------------------------------
    // Delete Tag
    // ----------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTag(TagDeleteForm form, DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        ApplyUserSelection(form.UserEmail ?? documentQuery.UserEmail, documentQuery.UserEmail);
        var tagId = ParseRequiredGuid(form.TagId, nameof(form.TagId));

        if (!ModelState.IsValid || tagId is null)
        {
            return View("Index", await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                tagDelete: form,
                cancellationToken: cancellationToken
            ));
        }

        var deleted = await _client.DeleteTagAsync(tagId.Value, cancellationToken);

        return View("Index", await BuildPageViewModelAsync(
            BuildDefaultUploadForm(),
            documentQuery: documentQuery,
            tagMessage: deleted ? "Đã xoá tag." : "Không tìm thấy hoặc không xoá được tag.",
            cancellationToken: cancellationToken,
            tagDelete: new TagDeleteForm()
        ));
    }

    // ----------------------------------------------------
    // Update Document
    // ----------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateDocument(DocumentUpdateForm form, DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        ApplyUserSelection(form.UserEmail ?? documentQuery.UserEmail, documentQuery.UserEmail);
        var documentId = ParseRequiredGuid(form.DocumentId, nameof(form.DocumentId));
        var groupId = form.UpdateGroup ? ParseGuidOrNull(form.GroupId, nameof(form.GroupId)) : null;

        if (!ModelState.IsValid || documentId is null)
        {
            return View("Index", await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                documentUpdate: form,
                cancellationToken: cancellationToken
            ));
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

        return View("Index", await BuildPageViewModelAsync(
            BuildDefaultUploadForm(),
            documentQuery: documentQuery,
            documentMessage: message,
            cancellationToken: cancellationToken,
            documentUpdate: new DocumentUpdateForm()
        ));
    }

    // ----------------------------------------------------
    // Delete Document
    // ----------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(DocumentDeleteForm form, DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        ApplyUserSelection(form.UserEmail ?? documentQuery.UserEmail, documentQuery.UserEmail);
        var documentId = ParseRequiredGuid(form.DocumentId, nameof(form.DocumentId));

        if (!ModelState.IsValid || documentId is null)
        {
            return View("Index", await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                documentDelete: form,
                cancellationToken: cancellationToken
            ));
        }

        var deleted = await _client.DeleteDocumentAsync(documentId.Value, cancellationToken);
        var message = deleted
            ? "Đã xoá document."
            : "Không tìm thấy document hoặc không đủ quyền xoá.";

        return View("Index", await BuildPageViewModelAsync(
            BuildDefaultUploadForm(),
            documentQuery: documentQuery,
            documentMessage: message,
            cancellationToken: cancellationToken,
            documentDelete: new DocumentDeleteForm()
        ));
    }

    // ----------------------------------------------------
    // Document Details & Download
    // ----------------------------------------------------
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> GetDocumentDetail(DocumentDetailForm form, DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        ApplyUserSelection(form.UserEmail ?? documentQuery.UserEmail, documentQuery.UserEmail);
        var documentId = ParseRequiredGuid(form.DocumentId, nameof(form.DocumentId));

        if (!ModelState.IsValid || documentId is null)
        {
            return View("Index", await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                documentDetail: form,
                cancellationToken: cancellationToken
            ));
        }

        var document = await _client.GetDocumentAsync(documentId.Value, cancellationToken);

        if (document is null)
        {
            return View("Index", await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                documentMessage: "Không tìm thấy tài liệu.",
                documentDetail: new DocumentDetailForm { UserEmail = form.UserEmail ?? documentQuery.UserEmail },
                cancellationToken: cancellationToken
            ));
        }

        Uri? downloadUri = null;
        if (document.LatestVersion is { } version)
        {
            downloadUri = await _client.GetDownloadUriAsync(version.Id, cancellationToken);
        }

        return View("Index", await BuildPageViewModelAsync(
            BuildDefaultUploadForm(),
            documentQuery: documentQuery,
            documentMessage: "Đã tải chi tiết tài liệu.",
            documentDetail: new DocumentDetailForm { UserEmail = form.UserEmail ?? documentQuery.UserEmail },
            documentDetailResult: new DocumentDetailResult(document, downloadUri),
            cancellationToken: cancellationToken
        ));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DownloadVersion(VersionDownloadForm form, DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        ApplyUserSelection(form.UserEmail ?? documentQuery.UserEmail, documentQuery.UserEmail);
        var versionId = ParseRequiredGuid(form.VersionId, nameof(form.VersionId));

        if (!ModelState.IsValid || versionId is null)
        {
            return View("Index", await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                versionDownload: form,
                cancellationToken: cancellationToken
            ));
        }

        var download = await _client.DownloadVersionAsync(versionId.Value, cancellationToken);
        if (download is null)
        {
            return View("Index", await BuildPageViewModelAsync(
                BuildDefaultUploadForm(),
                documentQuery: documentQuery,
                documentMessage: "Không tìm thấy phiên bản hoặc không thể tải file.",
                versionDownload: new VersionDownloadForm { UserEmail = form.UserEmail ?? documentQuery.UserEmail },
                cancellationToken: cancellationToken
            ));
        }

        var fileName = download.FileName ?? $"version-{versionId}.bin";
        return File(download.Content, download.ContentType, fileName);
    }

    // ----------------------------------------------------
    // Helpers
    // ----------------------------------------------------
    private UploadFormModel BuildDefaultUploadForm() => new()
    {
        DocType = Options.DocType,
        Status = Options.Status,
        Sensitivity = Options.Sensitivity,
        Title = Options.Title,
        DocumentTypeId = Options.DocumentTypeId?.ToString(),
        UserEmail = Options.OnBehalfUserEmail,
    };

    private EcmUserConfiguration ApplyUserSelection(string? userEmail, string? fallbackEmail)
    {
        var resolvedEmail = userEmail ?? fallbackEmail ?? Options.OnBehalfUserEmail;
        return _userSelection.ApplySelection(Options, resolvedEmail);
    }

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
        DocumentDetailForm? documentDetail = null,
        DocumentDetailResult? documentDetailResult = null,
        VersionDownloadForm? versionDownload = null,
        UploadResultModel? result = null,
        IReadOnlyCollection<TagLabelDto>? tags = null,
        DocumentListResult? documentList = null,
        UserProfile? profile = null,
        CancellationToken cancellationToken = default)
    {
        documentQuery ??= new DocumentQueryForm();

        if (tags is null || documentList is null || profile is null)
        {
            var reference = await LoadReferenceDataAsync(documentQuery, cancellationToken);
            tags ??= reference.Tags;
            documentList ??= reference.Documents;
            profile ??= reference.Profile;
        }

        var currentUser = _userSelection.GetCurrentUser();

        form.UserEmail ??= Options.OnBehalfUserEmail ?? currentUser.Email;
        documentQuery.UserEmail ??= form.UserEmail;

        tagCreate ??= new TagCreateForm();
        tagCreate.UserEmail ??= documentQuery.UserEmail;

        tagUpdate ??= new TagUpdateForm();
        tagUpdate.UserEmail ??= documentQuery.UserEmail;

        tagDelete ??= new TagDeleteForm();
        tagDelete.UserEmail ??= documentQuery.UserEmail;

        documentUpdate ??= new DocumentUpdateForm();
        documentUpdate.UserEmail ??= documentQuery.UserEmail;

        documentDelete ??= new DocumentDeleteForm();
        documentDelete.UserEmail ??= documentQuery.UserEmail;

        documentDetail ??= new DocumentDetailForm();
        documentDetail.UserEmail ??= documentQuery.UserEmail;

        versionDownload ??= new VersionDownloadForm();
        versionDownload.UserEmail ??= documentQuery.UserEmail;

        return new UploadPageViewModel
        {
            BaseUrl = Options.BaseUrl,
            Users = [.. _userSelection
                .GetUsers()
                .Select(user => new EcmUserViewModel(user.Email ?? string.Empty, user.DisplayName, user.Email == currentUser.Email))],
            SelectedUserEmail = currentUser.Email ?? string.Empty,
            UsingApiKeyAuthentication = Options.ApiKey.Enabled,
            UsingSsoAuthentication = Options.Sso.Enabled,
            UsingOnBehalfAuthentication = Options.IsOnBehalfEnabled,
            OnBehalfUserEmail = Options.OnBehalfUserEmail,
            OnBehalfUserId = Options.OnBehalfUserId,
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
            DocumentDetail = documentDetail ?? new DocumentDetailForm(),
            DocumentDetailResult = documentDetailResult,
            VersionDownload = versionDownload ?? new VersionDownloadForm(),
            CurrentProfile = profile ?? result?.Profile,
        };
    }

    private async Task<ReferenceData> LoadReferenceDataAsync(
        DocumentQueryForm documentQuery,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<TagLabelDto> tags = [];
        DocumentListResult? documents = null;
        UserProfile? profile = null;

        try
        {
            profile = await _client.GetCurrentUserProfileAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Không thể tải profile người dùng.");
        }

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

        return new ReferenceData(tags, documents, profile);
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

    private async Task<IReadOnlyCollection<TagLabelDto>> ApplyTagsAsync(
        Guid documentId,
        List<Guid> tagIds,
        Guid appliedBy,
        IReadOnlyCollection<TagLabelDto> availableTags,
        CancellationToken cancellationToken)
    {
        if (tagIds.Count == 0)
        {
            return [];
        }

        var applied = new List<TagLabelDto>();

        foreach (var tagId in tagIds)
        {
            var assigned = await _client.AssignTagToDocumentAsync(documentId, tagId, appliedBy, cancellationToken);
            if (assigned)
            {
                var tag = availableTags.FirstOrDefault(item => item.Id == tagId);
                if (tag is not null)
                {
                    applied.Add(tag);
                }
            }
        }

        return applied;
    }

    private List<Guid> ParseGuidList(IEnumerable<string> values, string fieldName)
    {
        var results = new List<Guid>();

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (Guid.TryParse(value, out var parsed))
            {
                results.Add(parsed);
                continue;
            }

            ModelState.AddModelError(fieldName, "Giá trị tag không hợp lệ (yêu cầu GUID).");
        }

        return results;
    }

    private sealed record ReferenceData(
        IReadOnlyCollection<TagLabelDto> Tags,
        DocumentListResult? Documents,
        UserProfile? Profile);

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
