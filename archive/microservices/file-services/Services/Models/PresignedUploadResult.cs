namespace FileServices.Services.Models;

public sealed record PresignedUploadResult(string ObjectKey, Uri UploadUrl, TimeSpan ExpiresIn);
