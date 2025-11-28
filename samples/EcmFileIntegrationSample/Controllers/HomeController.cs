using Ecm.Sdk.Configuration;
using Ecm.Sdk.Models.Documents;
using Ecm.Sdk.Models.Tags;

using EcmFileIntegrationSample.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace EcmFileIntegrationSample.Controllers;

public class HomeController(
    IEcmIntegrationService ecmService,
    IOptionsSnapshot<EcmIntegrationOptions> options,
    ILogger<HomeController> logger,
    EcmUserSelection userSelection) : Controller
{
    private const string TagMessageKey = "TagMessage";
    private const string DocumentMessageKey = "DocumentMessage";
    private readonly IEcmIntegrationService _ecmService = ecmService;
    private readonly IOptionsSnapshot<EcmIntegrationOptions> _optionsSnapshot = options;
    private readonly ILogger<HomeController> _logger = logger;
    private readonly EcmUserSelection _userSelection = userSelection;

    private EcmIntegrationOptions Options => _optionsSnapshot.Value;

    // ----------------------------------------------------
    // Home
    // ----------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        ApplyUserSelection(null, null);
        var connection = BuildConnectionInfo();
        SetConnection(connection);

        var profile = await LoadProfileAsync(cancellationToken);

        return View(new HomePageViewModel
        {
            Connection = connection,
            Profile = profile,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SwitchUser(string userEmail, string? returnUrl)
    {
        ApplyUserSelection(userEmail, null);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Index));
    }

    // ----------------------------------------------------
    // Upload
    // ----------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Upload(CancellationToken cancellationToken)
    {
        ApplyUserSelection(null, null);
        return View(await BuildUploadViewModelAsync(BuildDefaultUploadForm(), cancellationToken: cancellationToken));
    }

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
            return View(await BuildUploadViewModelAsync(form, cancellationToken: cancellationToken));
        }

        var profile = await _ecmService.GetProfileAsync(cancellationToken);
        if (profile is null)
        {
            return View(await BuildUploadViewModelAsync(
                form,
                cancellationToken: cancellationToken,
                error: "Không lấy được thông tin người dùng từ ECM. Kiểm tra cấu hình ApiKey/SSO."));
        }

        ownerId ??= profile.Id;
        createdBy ??= profile.Id;

        var originalFileName = Path.GetFileName(form.File?.FileName ?? string.Empty);
        var tempFilePath = Path.Combine(
            Path.GetTempPath(),
            $"{Path.GetFileNameWithoutExtension(originalFileName)}-{Guid.NewGuid():N}{Path.GetExtension(originalFileName)}");

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
                FileName = originalFileName,
                ContentType = string.IsNullOrWhiteSpace(form.File!.ContentType) ? null : form.File.ContentType
            };

            var document = await _ecmService.UploadDocumentAsync(uploadRequest, cancellationToken);
            if (document is null)
            {
                return View(await BuildUploadViewModelAsync(
                    form,
                    cancellationToken: cancellationToken,
                    error: "ECM không trả về thông tin tài liệu sau khi upload."));
            }

            var downloadUri = document.LatestVersion is { } version
                ? await _ecmService.GetDownloadUriAsync(version.Id, cancellationToken)
                : null;

            IReadOnlyCollection<TagLabelDto> appliedTags = [];
            IReadOnlyCollection<TagLabelDto> tags = [];

            try
            {
                tags = await _ecmService.ListTagsAsync(cancellationToken);
                appliedTags = await _ecmService.AssignTagsAsync(document.Id, selectedTagIds, profile.Id, tags, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Không thể gán tag cho document sau khi upload.");
            }

            return View(await BuildUploadViewModelAsync(
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
                cancellationToken: cancellationToken));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Upload thất bại.");
            return View(await BuildUploadViewModelAsync(
                form,
                cancellationToken: cancellationToken,
                error: "Upload thất bại. Kiểm tra log để biết thêm chi tiết."));
        }
        finally
        {
            TryDeleteFile(tempFilePath);
        }
    }

    [HttpGet]
    public async Task<IActionResult> BulkUpload(CancellationToken cancellationToken)
    {
        ApplyUserSelection(null, null);
        return View(await BuildBulkUploadViewModelAsync(BuildDefaultBulkUploadForm(), cancellationToken: cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkUpload([Bind(Prefix = "Form")] BulkUploadFormModel form, CancellationToken cancellationToken)
    {
        var selectedUser = ApplyUserSelection(form.UserEmail, null);
        var documentTypeId = ParseGuidOrNull(form.DocumentTypeId, nameof(form.DocumentTypeId));
        var ownerId = ParseGuidOrNull(form.OwnerId, nameof(form.OwnerId));
        var createdBy = ParseGuidOrNull(form.CreatedBy, nameof(form.CreatedBy));
        var selectedTagIds = ParseGuidList(form.SelectedTagIds, nameof(form.SelectedTagIds));

        if (form.Files is null || form.Files.Count == 0)
        {
            ModelState.AddModelError(nameof(form.Files), "Cần ít nhất một file để upload.");
        }

        if (form.Files is not null)
        {
            for (var index = 0; index < form.Files.Count; index++)
            {
                var file = form.Files[index];
                if (file is null || file.Length == 0)
                {
                    ModelState.AddModelError(nameof(form.Files), $"File thứ {index + 1} trống hoặc không hợp lệ.");
                }
            }
        }

        if (!ModelState.IsValid)
        {
            return View(await BuildBulkUploadViewModelAsync(form, cancellationToken: cancellationToken));
        }

        var profile = await _ecmService.GetProfileAsync(cancellationToken);
        if (profile is null)
        {
            return View(await BuildBulkUploadViewModelAsync(
                form,
                cancellationToken: cancellationToken,
                error: "Không lấy được thông tin người dùng từ ECM. Kiểm tra cấu hình ApiKey/SSO."));
        }

        ownerId ??= profile.Id;
        createdBy ??= profile.Id;

        var tempFiles = new List<string>();
        var uploadFiles = new List<DocumentUploadFile>();

        try
        {
            foreach (var file in form.Files)
            {
                var originalFileName = Path.GetFileName(file.FileName ?? string.Empty);
                var tempFilePath = Path.Combine(
                    Path.GetTempPath(),
                    $"{Path.GetFileNameWithoutExtension(originalFileName)}-{Guid.NewGuid():N}{Path.GetExtension(originalFileName)}");

                await using (var stream = System.IO.File.Create(tempFilePath))
                {
                    await file.CopyToAsync(stream, cancellationToken);
                }

                tempFiles.Add(tempFilePath);
                uploadFiles.Add(new DocumentUploadFile(
                    tempFilePath,
                    string.IsNullOrWhiteSpace(file.FileName) ? null : file.FileName,
                    string.IsNullOrWhiteSpace(file.ContentType) ? null : file.ContentType));
            }

            var batchRequest = new DocumentBatchUploadRequest(
                ownerId.Value,
                createdBy.Value,
                string.IsNullOrWhiteSpace(form.DocType) ? Options.DocType : form.DocType,
                string.IsNullOrWhiteSpace(form.Status) ? Options.Status : form.Status,
                string.IsNullOrWhiteSpace(form.Sensitivity) ? Options.Sensitivity : form.Sensitivity,
                uploadFiles)
            {
                DocumentTypeId = documentTypeId,
                Title = string.IsNullOrWhiteSpace(form.Title) ? null : form.Title,
                FlowDefinition = string.IsNullOrWhiteSpace(form.FlowDefinition) ? null : form.FlowDefinition,
                TagIds = selectedTagIds,
            };

            var batchResult = await _ecmService.UploadDocumentsBatchAsync(batchRequest, cancellationToken);
            var tags = await _ecmService.ListTagsAsync(cancellationToken);

            return View(await BuildBulkUploadViewModelAsync(
                new BulkUploadFormModel
                {
                    DocType = form.DocType,
                    Status = form.Status,
                    Sensitivity = form.Sensitivity,
                    Title = form.Title,
                    FlowDefinition = form.FlowDefinition,
                    UserEmail = form.UserEmail ?? selectedUser.Email,
                    SelectedTagIds = form.SelectedTagIds,
                },
                result: new BulkUploadResultModel
                {
                    Documents = batchResult.Documents,
                    Failures = batchResult.Failures,
                    Profile = profile,
                },
                tags: tags,
                cancellationToken: cancellationToken));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Upload hàng loạt thất bại.");
            return View(await BuildBulkUploadViewModelAsync(
                form,
                cancellationToken: cancellationToken,
                error: "Upload thất bại. Kiểm tra log để biết thêm chi tiết."));
        }
        finally
        {
            foreach (var file in tempFiles)
            {
                TryDeleteFile(file);
            }
        }
    }

    // ----------------------------------------------------
    // Tags
    // ----------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Tags(Guid? editTagId, string? openForm, string? userEmail, CancellationToken cancellationToken)
    {
        ApplyUserSelection(userEmail, null);
        return View(await BuildTagPageViewModelAsync(
            null,
            null,
            null,
            TempData[TagMessageKey] as string,
            editTagId,
            openForm,
            cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTag(TagCreateForm form, CancellationToken cancellationToken)
    {
        ApplyUserSelection(form.UserEmail, null);
        var namespaceId = ParseGuidOrNull(form.NamespaceId, nameof(form.NamespaceId));
        var parentId = ParseGuidOrNull(form.ParentId, nameof(form.ParentId));

        if (!ModelState.IsValid)
        {
            return View("Tags", await BuildTagPageViewModelAsync(form, null, null, null, null, "create", cancellationToken));
        }

        var request = new TagCreateRequest(namespaceId, parentId, form.Name, form.SortOrder, form.Color, form.IconKey, null);
        await _ecmService.CreateTagAsync(request, cancellationToken);

        TempData[TagMessageKey] = "Đã tạo tag mới.";
        return RedirectToAction(nameof(Tags));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateTag(TagUpdateForm form, CancellationToken cancellationToken)
    {
        ApplyUserSelection(form.UserEmail, null);
        var tagId = ParseRequiredGuid(form.TagId, nameof(form.TagId));
        var namespaceId = ParseRequiredGuid(form.NamespaceId, nameof(form.NamespaceId));
        var parentId = ParseGuidOrNull(form.ParentId, nameof(form.ParentId));

        if (!ModelState.IsValid || tagId is null || namespaceId is null)
        {
            return View("Tags", await BuildTagPageViewModelAsync(null, form, null, null, null, "update", cancellationToken));
        }

        var request = new TagUpdateRequest(namespaceId.Value, parentId, form.Name, form.SortOrder, form.Color, form.IconKey, form.IsActive, null);
        var updated = await _ecmService.UpdateTagAsync(tagId.Value, request, cancellationToken);

        TempData[TagMessageKey] = updated is null ? "Không tìm thấy tag cần cập nhật." : "Đã cập nhật tag.";
        return RedirectToAction(nameof(Tags));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteTag(TagDeleteForm form, CancellationToken cancellationToken)
    {
        ApplyUserSelection(form.UserEmail, null);
        var tagId = ParseRequiredGuid(form.TagId, nameof(form.TagId));

        if (!ModelState.IsValid || tagId is null)
        {
            return View("Tags", await BuildTagPageViewModelAsync(null, null, form, null, tagId, "delete", cancellationToken));
        }

        var deleted = await _ecmService.DeleteTagAsync(tagId.Value, cancellationToken);
        TempData[TagMessageKey] = deleted ? "Đã xoá tag." : "Không tìm thấy hoặc không xoá được tag.";
        return RedirectToAction(nameof(Tags));
    }

    // ----------------------------------------------------
    // Documents
    // ----------------------------------------------------
    [HttpGet]
    public async Task<IActionResult> Documents([FromQuery] DocumentQueryForm? documentQuery, CancellationToken cancellationToken)
    {
        documentQuery ??= new DocumentQueryForm();
        ApplyUserSelection(documentQuery.UserEmail, documentQuery.UserEmail);
        var message = TempData[DocumentMessageKey] as string;
        return View(await BuildDocumentListViewModelAsync(documentQuery, message, cancellationToken));
    }

    [HttpPost]
    [ActionName("Documents")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DocumentsPost(DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        ApplyUserSelection(documentQuery.UserEmail, documentQuery.UserEmail);
        if (!ModelState.IsValid)
        {
            return View(await BuildDocumentListViewModelAsync(documentQuery, null, cancellationToken));
        }

        return View(await BuildDocumentListViewModelAsync(documentQuery, "Đã tải danh sách tài liệu theo bộ lọc.", cancellationToken));
    }

    [HttpGet]
    public async Task<IActionResult> DocumentDetail(Guid id, CancellationToken cancellationToken)
    {
        var detail = await LoadDocumentDetailAsync(id, cancellationToken);
        var message = detail is null ? "Không tìm thấy tài liệu." : null;
        return View(await BuildDocumentDetailViewModelAsync(detail, message));
    }

    [HttpGet]
    public async Task<IActionResult> DownloadDocument(Guid id, CancellationToken cancellationToken)
    {
        var detail = await LoadDocumentDetailAsync(id, cancellationToken);
        if (detail?.Document.LatestVersion is not { } version)
        {
            TempData[DocumentMessageKey] = "Không tìm thấy phiên bản mới nhất để tải.";
            return RedirectToAction(nameof(Documents));
        }

        var download = await _ecmService.DownloadVersionAsync(version.Id, cancellationToken);
        if (download is null)
        {
            TempData[DocumentMessageKey] = "Không thể tải file.";
            return RedirectToAction(nameof(Documents));
        }

        var fileName = download.FileName?.Trim('"');

        if (string.IsNullOrWhiteSpace(fileName))
        {
            var title = detail.Document.Title;
            var extension = Path.GetExtension(title);

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".bin";
            }

            fileName = string.IsNullOrWhiteSpace(title) ? $"document{extension}" : title;
        }

        fileName = Path.GetFileName(string.IsNullOrWhiteSpace(fileName) ? "document.bin" : fileName);

        return File(download.Content, download.ContentType, fileName);
    }

    [HttpGet]
    public async Task<IActionResult> EditDocument(Guid id, CancellationToken cancellationToken)
    {
        var detail = await LoadDocumentDetailAsync(id, cancellationToken);
        if (detail is null)
        {
            TempData[DocumentMessageKey] = "Không tìm thấy tài liệu cần cập nhật.";
            return RedirectToAction(nameof(Documents));
        }

        var form = new DocumentUpdateForm
        {
            DocumentId = detail.Document.Id.ToString(),
            Title = detail.Document.Title,
            Status = detail.Document.Status,
            Sensitivity = detail.Document.Sensitivity,
            GroupId = detail.Document.GroupId?.ToString(),
            UpdateGroup = detail.Document.GroupId is not null,
            UserEmail = _userSelection.GetCurrentUser().Email,
            SelectedTagIds = detail.Document.Tags?.Select(tag => tag.Id.ToString()).ToList() ?? [],
        };

        return View(await BuildDocumentEditViewModelAsync(form, detail, null, cancellationToken));
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditDocument(DocumentUpdateForm form, CancellationToken cancellationToken)
    {
        ApplyUserSelection(form.UserEmail, null);
        var selectedTagIds = ParseGuidList(form.SelectedTagIds, nameof(form.SelectedTagIds));
        var documentId = ParseRequiredGuid(form.DocumentId, nameof(form.DocumentId));
        var detail = documentId is not null
            ? await LoadDocumentDetailAsync(documentId.Value, cancellationToken)
            : null;

        if (!ModelState.IsValid || documentId is null)
        {
            return View(await BuildDocumentEditViewModelAsync(form, detail, null, cancellationToken));
        }

        var groupId = form.UpdateGroup ? ParseGuidOrNull(form.GroupId, nameof(form.GroupId)) : null;
        if (!ModelState.IsValid)
        {
            // KHÔNG khai báo lại biến detail nữa
            return View(await BuildDocumentEditViewModelAsync(form, detail, null, cancellationToken));
        }

        var request = new DocumentUpdateRequest
        {
            Title = form.Title,
            Status = form.Status,
            Sensitivity = form.Sensitivity,
            GroupId = groupId,
            HasGroupId = form.UpdateGroup,
        };

        var updated = await _ecmService.UpdateDocumentAsync(documentId.Value, request, cancellationToken);
        if (updated is null)
        {
            return View(await BuildDocumentEditViewModelAsync(
                form,
                detail,
                "Không thể cập nhật document (không tồn tại hoặc không đủ quyền).",
                cancellationToken));
        }

        await UpdateDocumentTagsAsync(documentId.Value, detail?.Document.Tags, selectedTagIds, cancellationToken);

        TempData[DocumentMessageKey] = "Đã cập nhật document.";
        return RedirectToAction(nameof(Documents));
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocument(DocumentDeleteForm form, CancellationToken cancellationToken)
    {
        ApplyUserSelection(form.UserEmail, null);
        var documentId = ParseRequiredGuid(form.DocumentId, nameof(form.DocumentId));

        if (!ModelState.IsValid || documentId is null)
        {
            return View("Documents", await BuildDocumentListViewModelAsync(
                new DocumentQueryForm { UserEmail = form.UserEmail },
                "Không thể xoá document do thông tin không hợp lệ.",
                cancellationToken));
        }

        var deleted = await _ecmService.DeleteDocumentAsync(documentId.Value, cancellationToken);
        TempData[DocumentMessageKey] = deleted
            ? "Đã xoá document."
            : "Không tìm thấy hoặc không xoá được document.";

        return RedirectToAction(nameof(Documents), new { form.UserEmail });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteDocumentVersion(DocumentVersionDeleteForm form, CancellationToken cancellationToken)
    {
        ApplyUserSelection(form.UserEmail, null);
        var versionId = ParseRequiredGuid(form.VersionId, nameof(form.VersionId));

        if (!ModelState.IsValid || versionId is null)
        {
            return View("Documents", await BuildDocumentListViewModelAsync(
                new DocumentQueryForm { UserEmail = form.UserEmail },
                "Không thể xoá phiên bản do thông tin không hợp lệ.",
                cancellationToken));
        }

        var deleted = await _ecmService.DeleteDocumentByVersionAsync(versionId.Value, cancellationToken);
        TempData[DocumentMessageKey] = deleted
            ? "Đã xoá phiên bản tài liệu."
            : "Không tìm thấy hoặc không xoá được phiên bản.";

        return RedirectToAction(nameof(Documents), new { form.UserEmail });
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
        UserEmail = _userSelection.GetCurrentUser().Email,
    };

    private BulkUploadFormModel BuildDefaultBulkUploadForm() => new()
    {
        DocType = Options.DocType,
        Status = Options.Status,
        Sensitivity = Options.Sensitivity,
        Title = Options.Title,
        DocumentTypeId = Options.DocumentTypeId?.ToString(),
        UserEmail = _userSelection.GetCurrentUser().Email,
    };

    private EcmUserConfiguration ApplyUserSelection(string? userEmail, string? fallbackEmail)
    {
        var resolvedEmail = userEmail ?? fallbackEmail;
        return _userSelection.ApplySelection(Options, resolvedEmail);
    }

    private ConnectionInfoViewModel BuildConnectionInfo()
    {
        var currentUser = _userSelection.GetCurrentUser();

        return new ConnectionInfoViewModel
        {
            BaseUrl = Options.BaseUrl,
            Users = [.. _userSelection
                .GetUsers()
                .Select(user => new EcmUserViewModel(user.Email ?? string.Empty, user.DisplayName, user.Email == currentUser.Email))],
            SelectedUserEmail = currentUser.Email ?? string.Empty,
            SelectedUserDisplayName = currentUser.DisplayName,
            UsingApiKeyAuthentication = Options.ApiKey.Enabled,
            UsingSsoAuthentication = Options.Sso.Enabled,
        };
    }

    private void SetConnection(ConnectionInfoViewModel connection)
    {
        ViewBag.Connection = connection;
    }

    private async Task<UploadPageViewModel> BuildUploadViewModelAsync(
        UploadFormModel form,
        UploadResultModel? result = null,
        IReadOnlyCollection<TagLabelDto>? tags = null,
        string? error = null,
        CancellationToken cancellationToken = default)
    {
        form.UserEmail ??= _userSelection.GetCurrentUser().Email;

        tags ??= await LoadTagsAsync(cancellationToken);
        var profile = await LoadProfileAsync(cancellationToken);

        var connection = BuildConnectionInfo();
        SetConnection(connection);

        return new UploadPageViewModel
        {
            Connection = connection,
            Form = form,
            Result = result,
            Error = error,
            Tags = tags,
            CurrentProfile = profile ?? result?.Profile,
        };
    }

    private async Task<BulkUploadPageViewModel> BuildBulkUploadViewModelAsync(
        BulkUploadFormModel form,
        BulkUploadResultModel? result = null,
        IReadOnlyCollection<TagLabelDto>? tags = null,
        string? error = null,
        CancellationToken cancellationToken = default)
    {
        form.UserEmail ??= _userSelection.GetCurrentUser().Email;

        tags ??= await LoadTagsAsync(cancellationToken);
        var profile = await LoadProfileAsync(cancellationToken);

        var connection = BuildConnectionInfo();
        SetConnection(connection);

        return new BulkUploadPageViewModel
        {
            Connection = connection,
            Form = form,
            Result = result,
            Error = error,
            Tags = tags,
            CurrentProfile = profile ?? result?.Profile,
        };
    }

    private async Task<TagPageViewModel> BuildTagPageViewModelAsync(
        TagCreateForm? tagCreate,
        TagUpdateForm? tagUpdate,
        TagDeleteForm? tagDelete,
        string? message,
        Guid? editTagId,
        string? focusForm,
        CancellationToken cancellationToken)
    {
        var tags = await LoadTagsAsync(cancellationToken);
        var connection = BuildConnectionInfo();
        SetConnection(connection);

        tagCreate ??= new TagCreateForm();
        tagCreate.UserEmail ??= connection.SelectedUserEmail;

        tagUpdate ??= new TagUpdateForm();
        tagUpdate.UserEmail ??= connection.SelectedUserEmail;

        if (editTagId is not null)
        {
            var selected = tags.FirstOrDefault(tag => tag.Id == editTagId);
            if (selected is not null)
            {
                tagUpdate.TagId = selected.Id.ToString();
                tagUpdate.NamespaceId = selected.NamespaceId.ToString();
                tagUpdate.ParentId = selected.ParentId?.ToString();
                tagUpdate.Name = selected.Name;
                tagUpdate.SortOrder = selected.SortOrder;
                tagUpdate.Color = selected.Color;
                tagUpdate.IconKey = selected.IconKey;
                tagUpdate.IsActive = selected.IsActive;
            }
        }

        tagDelete ??= new TagDeleteForm();
        tagDelete.UserEmail ??= connection.SelectedUserEmail;
        if (editTagId is not null)
        {
            tagDelete.TagId = editTagId.ToString();
        }

        return new TagPageViewModel
        {
            Connection = connection,
            Tags = tags,
            Message = message,
            TagCreate = tagCreate,
            TagUpdate = tagUpdate,
            TagDelete = tagDelete,
            FocusForm = focusForm,
        };
    }

    private async Task<DocumentListPageViewModel> BuildDocumentListViewModelAsync(
        DocumentQueryForm documentQuery,
        string? message,
        CancellationToken cancellationToken)
    {
        documentQuery.UserEmail ??= _userSelection.GetCurrentUser().Email;

        var documents = await LoadDocumentsAsync(documentQuery, cancellationToken);
        var connection = BuildConnectionInfo();
        SetConnection(connection);

        return new DocumentListPageViewModel
        {
            Connection = connection,
            DocumentQuery = documentQuery,
            DocumentList = documents,
            DocumentMessage = message ?? TempData[DocumentMessageKey] as string,
            DeleteDocument = new DocumentDeleteForm { UserEmail = documentQuery.UserEmail },
            DeleteVersion = new DocumentVersionDeleteForm { UserEmail = documentQuery.UserEmail },
        };
    }

    private Task<DocumentDetailPageViewModel> BuildDocumentDetailViewModelAsync(
        DocumentDetailResult? detail,
        string? message
        )
    {
        var connection = BuildConnectionInfo();
        SetConnection(connection);

        return Task.FromResult(new DocumentDetailPageViewModel
        {
            Connection = connection,
            Detail = detail,
            Message = message,
        });
    }

    private async Task<DocumentEditPageViewModel> BuildDocumentEditViewModelAsync(
        DocumentUpdateForm form,
        DocumentDetailResult? detail,
        string? message,
        CancellationToken cancellationToken)
    {
        var connection = BuildConnectionInfo();
        SetConnection(connection);

        form.UserEmail ??= connection.SelectedUserEmail;

        detail ??= form.DocumentId is { Length: > 0 } && Guid.TryParse(form.DocumentId, out var documentId)
            ? await LoadDocumentDetailAsync(documentId, cancellationToken)
            : null;

        var tags = await LoadTagsAsync(cancellationToken);

        if (detail?.Document.Tags is { Count: > 0 } && form.SelectedTagIds.Count == 0)
        {
            form.SelectedTagIds = [.. detail.Document.Tags.Select(tag => tag.Id.ToString())];
        }

        return new DocumentEditPageViewModel
        {
            Connection = connection,
            Form = form,
            Detail = detail,
            Tags = tags,
            Message = message,
        };
    }

    private async Task<UserProfile?> LoadProfileAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _ecmService.GetProfileAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Không thể tải profile người dùng.");
            return null;
        }
    }

    private async Task<IReadOnlyCollection<TagLabelDto>> LoadTagsAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await _ecmService.ListTagsAsync(cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Không thể tải danh sách tag.");
            return [];
        }
    }

    private async Task<DocumentListResult?> LoadDocumentsAsync(DocumentQueryForm documentQuery, CancellationToken cancellationToken)
    {
        try
        {
            var query = BuildDocumentListQuery(documentQuery);
            return await _ecmService.ListDocumentsAsync(query, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Không thể tải danh sách document.");
            return null;
        }
    }

    private async Task<DocumentDetailResult?> LoadDocumentDetailAsync(Guid documentId, CancellationToken cancellationToken)
    {
        try
        {
            var document = await _ecmService.GetDocumentAsync(documentId, cancellationToken);
            if (document is null)
            {
                return null;
            }

            Uri? downloadUri = null;
            if (document.LatestVersion is { } version)
            {
                downloadUri = await _ecmService.GetDownloadUriAsync(version.Id, cancellationToken);
            }

            return new DocumentDetailResult(document, downloadUri);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Không thể tải chi tiết tài liệu {DocumentId}.", documentId);
            return null;
        }
    }

    private async Task UpdateDocumentTagsAsync(
        Guid documentId,
        IEnumerable<DocumentTagDto>? existingTags,
        IEnumerable<Guid> selectedTagIds,
        CancellationToken cancellationToken)
    {
        var desiredTags = selectedTagIds.Distinct().ToHashSet();
        var currentTags = existingTags?.Select(tag => tag.Id).ToHashSet() ?? [];

        var tagsToRemove = currentTags.Except(desiredTags).ToList();
        var tagsToAdd = desiredTags.Except(currentTags).ToList();

        foreach (var tagId in tagsToRemove)
        {
            try
            {
                await _ecmService.RemoveTagFromDocumentAsync(documentId, tagId, cancellationToken);
            }
            catch (Exception exception)
            {
                _logger.LogWarning(exception, "Không thể xoá tag {TagId} khỏi document {DocumentId}.", tagId, documentId);
            }
        }

        if (tagsToAdd.Count == 0)
        {
            return;
        }

        try
        {
            var tags = await LoadTagsAsync(cancellationToken);
            var profile = await _ecmService.GetProfileAsync(cancellationToken);
            await _ecmService.AssignTagsAsync(documentId, tagsToAdd, profile?.Id, tags, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Không thể gán tag cho document {DocumentId}.", documentId);
        }
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
