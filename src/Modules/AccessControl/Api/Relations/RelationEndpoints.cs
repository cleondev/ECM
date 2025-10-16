using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.AccessControl.Application.Relations;
using ECM.AccessControl.Api;
using ECM.AccessControl.Application.Relations.Commands;
using ECM.AccessControl.Application.Relations.Queries;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;

namespace ECM.AccessControl.Api.Relations;

public static class RelationEndpoints
{
    public static RouteGroupBuilder MapRelationEndpoints(this IEndpointRouteBuilder builder)
    {
        var group = builder.MapGroup("/api/access-control/relations");
        group.WithTags("Access Control - Relations");
        group.WithGroupName(AccessControlSwagger.DocumentName);
        group.RequireAuthorization();

        group.MapGet("/subjects/{subjectId:guid}", GetBySubjectAsync)
            .WithName("GetRelationsBySubject")
            .WithSummary("List relations for a subject");

        group.MapGet("/objects/{objectType}/{objectId:guid}", GetByObjectAsync)
            .WithName("GetRelationsByObject")
            .WithSummary("List relations for an object");

        group.MapPost("/", CreateRelationAsync)
            .WithName("CreateRelation")
            .WithSummary("Create a relation");

        group.MapDelete("/subjects/{subjectId:guid}/objects/{objectType}/{objectId:guid}", DeleteRelationAsync)
            .WithName("DeleteRelation")
            .WithSummary("Delete a relation");

        return group;
    }

    private static async Task<Ok<IReadOnlyCollection<AccessRelationResponse>>> GetBySubjectAsync(
        Guid subjectId,
        GetAccessRelationsBySubjectQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var relations = await handler.HandleAsync(new GetAccessRelationsBySubjectQuery(subjectId), cancellationToken);
        var response = relations.Select(MapToResponse).ToArray();
        return TypedResults.Ok<IReadOnlyCollection<AccessRelationResponse>>(response);
    }

    private static async Task<Ok<IReadOnlyCollection<AccessRelationResponse>>> GetByObjectAsync(
        string objectType,
        Guid objectId,
        GetAccessRelationsByObjectQueryHandler handler,
        CancellationToken cancellationToken)
    {
        var relations = await handler.HandleAsync(new GetAccessRelationsByObjectQuery(objectType, objectId), cancellationToken);
        var response = relations.Select(MapToResponse).ToArray();
        return TypedResults.Ok<IReadOnlyCollection<AccessRelationResponse>>(response);
    }

    private static async Task<Results<Created<AccessRelationResponse>, ValidationProblem>> CreateRelationAsync(
        CreateAccessRelationRequest request,
        CreateAccessRelationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(
            new CreateAccessRelationCommand(
                request.SubjectId,
                request.ObjectType,
                request.ObjectId,
                request.Relation),
            cancellationToken);

        if (result.IsFailure || result.Value is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                ["relation"] = [.. result.Errors]
            });
        }

        var response = MapToResponse(result.Value);
        return TypedResults.Created("/api/access-control/relations", response);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteRelationAsync(
        Guid subjectId,
        string objectType,
        Guid objectId,
        string relation,
        DeleteAccessRelationCommandHandler handler,
        CancellationToken cancellationToken)
    {
        var deleted = await handler.HandleAsync(
            new DeleteAccessRelationCommand(subjectId, objectType, objectId, relation),
            cancellationToken);

        if (!deleted)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }

    private static AccessRelationResponse MapToResponse(AccessRelationSummary summary)
        => new(
            summary.SubjectId,
            summary.ObjectType,
            summary.ObjectId,
            summary.Relation,
            summary.CreatedAtUtc);
}
