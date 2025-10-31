using System;
using ECM.Document.Domain.Documents;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ECM.Document.Infrastructure.Persistence.Configurations;

public static class EfConverters
{
    public static readonly ValueConverter<DocumentId, Guid> DocumentIdConverter =
        new(
            id => id.Value,
            value => DocumentId.FromGuid(value)
        );

    public static readonly ValueComparer<DocumentId> DocumentIdComparer =
        new(
            (left, right) => left.Value == right.Value,
            id => id.Value.GetHashCode(),
            id => DocumentId.FromGuid(id.Value)
        );

    public static readonly ValueConverter<DocumentTitle, string> DocumentTitleConverter =
        new(
            title => title.Value,
            value => DocumentTitle.Create(value)
        );

    public static readonly ValueComparer<DocumentTitle> DocumentTitleComparer =
        new ValueComparer<DocumentTitle>(
            (left, right) =>
                (left == null && right == null) ||
                (left != null && right != null && left.Value == right.Value),
            v => v == null ? 0 : v.Value.GetHashCode(),
            v => v == null ? null : DocumentTitle.Create(v.Value)
        );
}
