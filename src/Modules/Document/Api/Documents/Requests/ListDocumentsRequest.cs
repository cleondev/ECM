using System;
using Microsoft.AspNetCore.Mvc;

namespace ECM.Document.Api.Documents.Requests;

public sealed record ListDocumentsRequest(
    [property: FromQuery(Name = "q")] string? Query,
    [property: FromQuery(Name = "doc_type")] string? DocType,
    [property: FromQuery(Name = "status")] string? Status,
    [property: FromQuery(Name = "sensitivity")] string? Sensitivity,
    [property: FromQuery(Name = "owner_id")] Guid? OwnerId,
    [property: FromQuery(Name = "group_id")] Guid? GroupId,
    [property: FromQuery(Name = "group_ids")] Guid[]? GroupIds,
    [property: FromQuery(Name = "tags[]")] Guid[]? Tags,
    [property: FromQuery] int Page = 1,
    [property: FromQuery] int PageSize = 24,
    [property: FromQuery] string? Sort = null);
