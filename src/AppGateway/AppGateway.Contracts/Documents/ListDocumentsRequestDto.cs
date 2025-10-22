using System;
using System.ComponentModel.DataAnnotations;

namespace AppGateway.Contracts.Documents;

public sealed class ListDocumentsRequestDto
{
    private const int DefaultPageSize = 24;
    private const int MaxPageSize = 200;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = DefaultPageSize;

    public string? DocType { get; init; }

    public string? Status { get; init; }

    public string? Sensitivity { get; init; }

    public Guid? OwnerId { get; init; }

    public string? Department { get; init; }

    public Guid[]? Tags { get; init; }

    public static readonly ListDocumentsRequestDto Default = new();
}
