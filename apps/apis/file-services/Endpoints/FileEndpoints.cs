using System.IO;

using FileServices.Endpoints.Requests;
using FileServices.Services;

namespace FileServices.Endpoints;

public static class FileEndpoints
{
    public static RouteGroupBuilder MapFileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/files");
        group.WithTags("Files");

        group.MapPost("/presign-upload", GeneratePresignedUploadUrl)
             .WithName("GeneratePresignedUploadUrl")
             .WithDescription("Generate a presigned URL that can be used to upload a file directly to object storage.")
             .Produces(StatusCodes.Status200OK)
             .ProducesValidationProblem();

        return group;
    }

    private static async Task<IResult> GeneratePresignedUploadUrl(
        GenerateUploadUrlRequest request,
        IFileStorageService fileStorageService,
        TimeProvider timeProvider,
        CancellationToken cancellationToken)
    {
        var safeFileName = Path.GetFileName(request.FileName);
        if (string.IsNullOrWhiteSpace(safeFileName))
        {
            safeFileName = "upload.bin";
        }
        var timestamp = timeProvider.GetUtcNow();
        var objectKey = $"uploads/{timestamp:yyyy/MM/dd}/{Guid.NewGuid():N}-{safeFileName}";

        var result = await fileStorageService.GenerateUploadUrlAsync(objectKey, request.ContentType, cancellationToken).ConfigureAwait(false);

        return Results.Ok(new
        {
            objectKey = result.ObjectKey,
            uploadUrl = result.UploadUrl,
            expiresInSeconds = (int)result.ExpiresIn.TotalSeconds,
        });
    }
}
