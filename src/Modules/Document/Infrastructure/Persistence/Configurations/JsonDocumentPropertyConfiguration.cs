using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ECM.Document.Infrastructure.Persistence.Configurations;

internal static class JsonDocumentPropertyConfiguration
{
    private static readonly JsonDocumentOptions Options = new();

    private static readonly ValueConverter<JsonDocument, string> Converter = new(
        document => ToRawJson(document),
        json => FromJson(json));

    private static readonly ValueComparer<JsonDocument> Comparer = new(
        (left, right) =>
            string.Equals(ToRawJson(left), ToRawJson(right), StringComparison.Ordinal),
        value =>
            StringComparer.Ordinal.GetHashCode(ToRawJson(value)),
        value =>
            JsonDocument.Parse(ToRawJson(value), Options));

    public static PropertyBuilder<JsonDocument> ConfigureJsonDocument(
        this PropertyBuilder<JsonDocument> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasColumnType("jsonb");
        builder.HasDefaultValueSql("'{}'::jsonb");
        builder.HasConversion(Converter);
        builder.Metadata.SetValueComparer(Comparer);

        return builder;
    }

    private static string ToRawJson(JsonDocument? document)
        => document?.RootElement.GetRawText() ?? "{}";

    private static JsonDocument FromJson(string? json)
        => string.IsNullOrWhiteSpace(json)
            ? JsonDocument.Parse("{}", Options)
            : JsonDocument.Parse(json, Options);
}
