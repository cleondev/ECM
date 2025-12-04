using ECM.Document.Api.Tags.Responses;
using ECM.Document.Application.Tags.Results;

namespace ECM.Document.Api.Tags;

internal static class TagEndpointMapping
{
    internal const string DefaultColor = "#FBBF24";
    internal const string UserDefaultIconKey = "🏷️";
    internal const string ManagementDefaultIconKey = "📁";

    internal static TagLabelResponse ToResponse(TagLabelResult tag, string defaultIconKey)
    {
        return new TagLabelResponse(
            tag.Id,
            tag.NamespaceId,
            tag.NamespaceScope,
            tag.NamespaceDisplayName,
            tag.ParentId,
            tag.Name,
            tag.PathIds,
            tag.SortOrder,
            NormalizeColor(tag.Color),
            NormalizeIcon(tag.IconKey, defaultIconKey),
            tag.IsActive,
            tag.IsSystem,
            tag.CreatedBy,
            tag.CreatedAtUtc
        );
    }

    internal static string NormalizeColor(string? color)
        => string.IsNullOrWhiteSpace(color) ? DefaultColor : color.Trim();

    internal static string NormalizeIcon(string? iconKey, string defaultIconKey)
        => string.IsNullOrWhiteSpace(iconKey) ? defaultIconKey : iconKey.Trim();
}
