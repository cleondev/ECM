using System;
using System.ComponentModel.DataAnnotations;

namespace AppGateway.Contracts.Documents;

public sealed class ListDocumentsRequestDto
{
    private const int DefaultPageSize = 24;
    private const int MaxPageSize = 200;

    public static class QueryKeys
    {
        public const string DocType = "doc_type";
        public const string Status = "status";
        public const string Sensitivity = "sensitivity";
        public const string Query = "q";
        public const string OwnerId = "owner_id";
        public const string GroupId = "group_id";
        public const string GroupIds = "group_ids";
        public const string Tags = "tags[]";
        public const string Sort = "sort";
    }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;

    public string? DocType { get; init; }

    public string? Status { get; init; }

    public string? Sensitivity { get; init; }

    public string? Query { get; init; }

    public Guid? OwnerId { get; init; }

    public Guid? GroupId { get; init; }

    public Guid[]? GroupIds { get; init; }

    public Guid[]? Tags { get; init; }

    public string? Sort { get; init; }

    public static readonly ListDocumentsRequestDto Default = new();
}
