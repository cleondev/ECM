using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;

namespace AppGateway.Contracts.Documents;

public sealed class ListDocumentsRequestDto
{
    private const int DefaultPageSize = 24;
    private const int MaxPageSize = 200;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;

    [FromQuery(Name = "doc_type")]
    public string? DocType { get; init; }

    [FromQuery(Name = "status")]
    public string? Status { get; init; }

    [FromQuery(Name = "sensitivity")]
    public string? Sensitivity { get; init; }

    [FromQuery(Name = "q")]
    public string? Query { get; init; }

    [FromQuery(Name = "owner_id")]
    public Guid? OwnerId { get; init; }

    [FromQuery(Name = "group_id")]
    public Guid? GroupId { get; init; }

    [FromQuery(Name = "group_ids")]
    public Guid[]? GroupIds { get; init; }

    [FromQuery(Name = "tags[]")]
    public Guid[]? Tags { get; init; }

    [FromQuery(Name = "sort")]
    public string? Sort { get; init; }

    public static readonly ListDocumentsRequestDto Default = new();
}
