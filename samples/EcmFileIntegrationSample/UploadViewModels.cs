using System.ComponentModel.DataAnnotations;

namespace samples.EcmFileIntegrationSample;

public sealed class UploadFormModel
{
    [Display(Name = "Doc type")]
    public string DocType { get; set; } = "General";

    [Display(Name = "Status")]
    public string Status { get; set; } = "Draft";

    [Display(Name = "Sensitivity")]
    public string Sensitivity { get; set; } = "Internal";

    [Display(Name = "Owner ID")]
    public string? OwnerId { get; set; }

    [Display(Name = "Created by")]
    public string? CreatedBy { get; set; }

    [Display(Name = "Document type ID")]
    public string? DocumentTypeId { get; set; }

    [Display(Name = "Tiêu đề")]
    public string? Title { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn file để upload.")]
    [Display(Name = "File")]
    public IFormFile? File { get; set; }
}

public sealed class UploadResultModel
{
    public required DocumentDto Document { get; init; }

    public Uri? DownloadUri { get; init; }

    public UserProfile? Profile { get; init; }
}

public sealed class UploadPageViewModel
{
    public string BaseUrl { get; init; } = string.Empty;

    public bool HasAccessToken { get; init; }

    public bool UsingOnBehalfAuthentication { get; init; }

    public Guid? OnBehalfUserId { get; init; }

    public string? OnBehalfUserEmail { get; init; }

    public UploadFormModel Form { get; init; } = new();

    public UploadResultModel? Result { get; init; }

    public string? Error { get; init; }
}
