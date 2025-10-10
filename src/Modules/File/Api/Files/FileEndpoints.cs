using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        group.MapGet("/", GetRecentFiles)
             .WithName("GetRecentFiles")
             .WithDescription("Returns the latest uploaded files.");

        group.MapPost("/", RegisterFileAsync)
             .WithName("RegisterFile")
             .WithDescription("Register a file stored in MinIO through the file module.");

        return group;
    }

    private static async Task<Ok<IReadOnlyCollection<FileEntry>>> GetRecentFiles(FileApplicationService service, CancellationToken cancellationToken)
    {
        var files = await service.GetRecentAsync(10, cancellationToken);
        return TypedResults.Ok(files);
    }

    private static async Task<Results<Created<FileEntry>, ValidationProblem>> RegisterFileAsync(RegisterFileRequest request, FileApplicationService service, CancellationToken cancellationToken)
    {
        var command = new RegisterFileCommand(request.FileName, request.ContentType, request.Size, request.StorageKey);
        var result = await service.RegisterAsync(command, cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["file"] = result.Errors.ToArray()
            });
        }

        return TypedResults.Created($"/api/ecm/files/{result.Value.Id}", result.Value);
    }
}
