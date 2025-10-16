using ECM.Abstractions.Files;
using ECM.File.Api;
using ECM.File.Application.Files;
using ECM.File.Domain.Files;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ECM.File.Api.Files;

public static class FileEndpoints
{
    public static RouteGroupBuilder MapFileEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/ecm/files");
        group.WithTags("Files");
        group.WithGroupName(FileSwagger.DocumentName);

        group.MapGet("/", GetRecentFiles)
             .WithName("GetRecentFiles")
             .WithDescription("Returns the latest uploaded files.");

        group.MapPost("/", UploadFileAsync)
             .WithName("UploadFile")
             .WithDescription("Uploads a file to object storage and registers metadata.");

        return group;
    }

    private static async Task<Ok<IReadOnlyCollection<StoredFile>>> GetRecentFiles(GetRecentFilesQueryHandler handler, CancellationToken cancellationToken)
    {
        var query = new GetRecentFilesQuery(10);
        var files = await handler.HandleAsync(query, cancellationToken);
        return TypedResults.Ok(files);
    }

    private static async Task<Results<Created<FileUploadResult>, ValidationProblem>> UploadFileAsync(IFormFile file, UploadFileCommandHandler handler, CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = ["A non-empty file is required."]
            });
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;

        await using var stream = file.OpenReadStream();
        var request = new FileUploadRequest(file.FileName, contentType, file.Length, stream);
        var result = await handler.UploadAsync(request, cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = [.. result.Errors]
            });
        }

        return TypedResults.Created($"/api/ecm/files/{result.Value.StorageKey}", result.Value);
    }
}
