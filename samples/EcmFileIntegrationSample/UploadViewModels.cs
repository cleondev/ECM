using System.ComponentModel.DataAnnotations;

using Ecm.Sdk.Models.Documents;
using Ecm.Sdk.Models.Tags;

namespace samples.EcmFileIntegrationSample;

public sealed class UploadFormModel
{
    [Display(Name = "Doc type")]
    public string DocType { get; set; } = "General";

    [Display(Name = "Status")]
    public string Status { get; set; } = "Draft";

    [Display(Name = "Sensitivity")]
    public string Sensitivity { get; set; } = "Internal";

    [Display(Name = "User email")]
    public string? UserEmail { get; set; }

    [Display(Name = "Owner ID")]
    public string? OwnerId { get; set; }

    [Display(Name = "Created by")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Document type ID")]
    public string? DocumentTypeId { get; set; }

    [Display(Name = "Tiêu đề")]
    public string? Title { get; set; }

    [Display(Name = "Tags")]
    public List<string> SelectedTagIds { get; set; } = new();

    [Required(ErrorMessage = "Vui lòng chọn file để upload.")]
    [Display(Name = "File")]
    public IFormFile? File { get; set; }
}

public sealed class UploadResultModel
{
    public required DocumentDto Document { get; init; }

    public Uri? DownloadUri { get; init; }

    public UserProfile? Profile { get; init; }

    public IReadOnlyCollection<TagLabelDto> AppliedTags { get; init; } = [];
}

public sealed class UploadPageViewModel
{
    public string BaseUrl { get; init; } = string.Empty;

    public IReadOnlyCollection<EcmUserViewModel> Users { get; init; } = [];

    public string SelectedUserEmail { get; init; } = string.Empty;

    public bool UsingApiKeyAuthentication { get; init; }

    public bool UsingSsoAuthentication { get; init; }

    public bool UsingOnBehalfAuthentication { get; init; }

    public Guid? OnBehalfUserId { get; init; }

    public string? OnBehalfUserEmail { get; init; }

    public UploadFormModel Form { get; init; } = new();

    public UploadResultModel? Result { get; init; }

    public string? Error { get; init; }

    public IReadOnlyCollection<TagLabelDto>? Tags { get; init; }

    public string? TagMessage { get; init; }

    public TagCreateForm TagCreate { get; init; } = new();

    public TagUpdateForm TagUpdate { get; init; } = new();

    public TagDeleteForm TagDelete { get; init; } = new();

    public DocumentQueryForm DocumentQuery { get; init; } = new();

    public DocumentListResult? DocumentList { get; init; }

    public string? DocumentMessage { get; init; }

    public DocumentUpdateForm DocumentUpdate { get; init; } = new();

    public DocumentDeleteForm DocumentDelete { get; init; } = new();

    public DocumentDetailForm DocumentDetail { get; init; } = new();

    public DocumentDetailResult? DocumentDetailResult { get; init; }

    public VersionDownloadForm VersionDownload { get; init; } = new();

    public UserProfile? CurrentProfile { get; init; }
}

public sealed record EcmUserViewModel(string Email, string DisplayName, bool IsSelected);

public sealed class TagCreateForm
{
    public string? UserEmail { get; set; }

    [Display(Name = "Namespace ID")]
    public string? NamespaceId { get; set; }

    [Display(Name = "Parent ID")]
    public string? ParentId { get; set; }

    [Required(ErrorMessage = "Tên tag bắt buộc.")]
    [Display(Name = "Tên tag")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Thứ tự")]
    public int? SortOrder { get; set; }

    [Display(Name = "Màu")]
    public string? Color { get; set; }

    [Display(Name = "Biểu tượng")]
    public string? IconKey { get; set; }
}

public sealed class TagUpdateForm
{
    public string? UserEmail { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Tag ID.")]
    [Display(Name = "Tag ID")]
    public string TagId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Namespace ID bắt buộc.")]
    [Display(Name = "Namespace ID")]
    public string NamespaceId { get; set; } = string.Empty;

    [Display(Name = "Parent ID")]
    public string? ParentId { get; set; }

    [Required(ErrorMessage = "Tên tag bắt buộc.")]
    [Display(Name = "Tên tag")]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Thứ tự")]
    public int? SortOrder { get; set; }

    [Display(Name = "Màu")]
    public string? Color { get; set; }

    [Display(Name = "Biểu tượng")]
    public string? IconKey { get; set; }

    [Display(Name = "Kích hoạt")]
    public bool IsActive { get; set; } = true;
}

public sealed class TagDeleteForm
{
    public string? UserEmail { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Tag ID cần xoá.")]
    [Display(Name = "Tag ID")]
    public string TagId { get; set; } = string.Empty;
}

public sealed class DocumentQueryForm
{
    public string? UserEmail { get; set; }

    [Display(Name = "Từ khóa")]
    public string? Query { get; set; }

    [Display(Name = "Doc type")]
    public string? DocType { get; set; }

    [Display(Name = "Status")]
    public string? Status { get; set; }

    [Display(Name = "Sensitivity")]
    public string? Sensitivity { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Trang phải >= 1")]
    [Display(Name = "Trang")]
    public int Page { get; set; } = 1;

    [Range(1, 200, ErrorMessage = "Page size 1-200")]
    [Display(Name = "Số mục/trang")]
    public int PageSize { get; set; } = 10;
}

public sealed class DocumentUpdateForm
{
    public string? UserEmail { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Document ID.")]
    [Display(Name = "Document ID")]
    public string DocumentId { get; set; } = string.Empty;

    [Display(Name = "Tiêu đề")]
    public string? Title { get; set; }

    [Display(Name = "Status")]
    public string? Status { get; set; }

    [Display(Name = "Sensitivity")]
    public string? Sensitivity { get; set; }

    [Display(Name = "Group ID")]
    public string? GroupId { get; set; }

    [Display(Name = "Cập nhật Group ID")]
    public bool UpdateGroup { get; set; }
}

public sealed class DocumentDeleteForm
{
    public string? UserEmail { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Document ID cần xoá.")]
    [Display(Name = "Document ID")]
    public string DocumentId { get; set; } = string.Empty;
}

public sealed class DocumentDetailForm
{
    public string? UserEmail { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Document ID.")]
    [Display(Name = "Document ID")]
    public string DocumentId { get; set; } = string.Empty;
}

public sealed class VersionDownloadForm
{
    public string? UserEmail { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập Version ID.")]
    [Display(Name = "Version ID")]
    public string VersionId { get; set; } = string.Empty;
}

public sealed record DocumentDetailResult(DocumentDto Document, Uri? DownloadUri);
