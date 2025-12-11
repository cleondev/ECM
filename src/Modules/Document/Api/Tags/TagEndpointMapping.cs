using System.Collections.Generic;

using ECM.Document.Api.Tags.Responses;
using ECM.Document.Application.Tags.Results;

namespace ECM.Document.Api.Tags;

internal static class TagEndpointMapping
{
    internal const string DefaultColor = "#FBBF24";
    internal const string UserDefaultIconKey = "🏷️";
    internal const string ManagementDefaultIconKey = "📁";

    private static readonly IReadOnlyDictionary<string, string> IconAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["tag"] = UserDefaultIconKey,
            ["label"] = UserDefaultIconKey,
            ["file"] = ManagementDefaultIconKey,
            ["folder"] = ManagementDefaultIconKey,
            ["briefcase"] = "💼",
            ["office"] = "🏢",
            ["paperclip"] = "📎",
            ["documents"] = "📂",
            ["organizer"] = "🗂️",
            ["archive"] = "🗃️",
            ["cabinet"] = "🗄️",
            ["clipboard"] = "📋",
            ["links"] = "🖇️",
            ["note"] = "🗒️",
            ["calendar"] = "📅",
            ["schedule"] = "🗓️",
            ["design"] = "🎨",
            ["report"] = "📊",
            ["chart"] = "📈",
            ["laptop"] = "💻",
            ["desktop"] = "🖥️",
            ["keyboard"] = "⌨️",
            ["mouse"] = "🖱️",
            ["tools"] = "🛠️",
            ["toolbox"] = "🧰",
            ["screwdriver"] = "🪛",
            ["announcement"] = "📢",
            ["idea"] = "💡",
            ["sparkles"] = "✨",
            ["star"] = "⭐",
            ["hot"] = "🔥",
            ["web"] = "🌐",
            ["compass"] = "🧭",
            ["key"] = "🔑",
            ["magic-tag"] = "🪄",
            ["library"] = "📚",
            ["brainstorm"] = "🧠",
            ["puzzle"] = "🧩",
            ["experiment"] = "🧪",
            ["microscope"] = "🔬",
            ["dna"] = "🧬",
            ["lab"] = "🧫",
            ["lotion"] = "🧴",
            ["film"] = "🎬",
            ["camera"] = "🎥",
            ["projector"] = "📽️",
            ["reel"] = "🎞️",
            ["studio"] = "🎙️",
            ["microphone"] = "🎤",
            ["headphones"] = "🎧",
            ["piano"] = "🎹",
            ["violin"] = "🎻",
            ["guitar"] = "🎸",
            ["music"] = "🎼",
            ["radio"] = "📻",
            ["television"] = "📺",
            ["photo"] = "📷",
            ["camera-flash"] = "📸",
            ["art"] = "🖼️",
            ["gaming"] = "🎮",
            ["phone"] = "📱",
            ["target"] = "🎯",
            ["gift"] = "🎁",
            ["rocket"] = "🚀",
            ["satellite"] = "🛰️",
            ["map"] = "🗺️",
            ["car"] = "🚗",
            ["taxi"] = "🚕",
            ["bus"] = "🚌",
            ["train"] = "🚆",
            ["metro"] = "🚇",
            ["airplane"] = "✈️",
            ["helicopter"] = "🚁",
            ["ship"] = "🚢",
            ["sailboat"] = "⛵",
            ["bike"] = "🚲",
            ["scooter"] = "🛴",
            ["moped"] = "🛵",
            ["motorcycle"] = "🏍️",
            ["tractor"] = "🚜",
            ["truck"] = "🚛",
            ["package"] = "📦",
            ["broom"] = "🧹",
            ["basket"] = "🧺",
            ["bucket"] = "🪣",
            ["sponge"] = "🧽",
        };

    private static readonly ISet<string> SupportedIcons =
        new HashSet<string>(StringComparer.Ordinal)
        {
            UserDefaultIconKey,
            ManagementDefaultIconKey,
            "💼",
            "🏢",
            "📎",
            "📂",
            "🗂️",
            "🗃️",
            "🗄️",
            "📋",
            "🖇️",
            "🗒️",
            "📅",
            "🗓️",
            "🎨",
            "📊",
            "📈",
            "💻",
            "🖥️",
            "⌨️",
            "🖱️",
            "🛠️",
            "🧰",
            "🪛",
            "📢",
            "💡",
            "✨",
            "⭐",
            "🔥",
            "🌐",
            "🧭",
            "🔑",
            "🪄",
            "📚",
            "🧠",
            "🧩",
            "🧪",
            "🔬",
            "🧬",
            "🧫",
            "🧴",
            "🎬",
            "🎥",
            "📽️",
            "🎞️",
            "🎙️",
            "🎤",
            "🎧",
            "🎹",
            "🎻",
            "🎸",
            "🎼",
            "📻",
            "📺",
            "📷",
            "📸",
            "🖼️",
            "🎮",
            "📱",
            "🎯",
            "🎁",
            "🚀",
            "🛰️",
            "🗺️",
            "🚗",
            "🚕",
            "🚌",
            "🚆",
            "🚇",
            "✈️",
            "🚁",
            "🚢",
            "⛵",
            "🚲",
            "🛴",
            "🛵",
            "🏍️",
            "🚜",
            "🚛",
            "📦",
            "🧹",
            "🧺",
            "🪣",
            "🧽",
        };

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
    {
        if (string.IsNullOrWhiteSpace(iconKey))
        {
            return defaultIconKey;
        }

        var trimmed = iconKey.Trim();

        if (IconAliases.TryGetValue(trimmed, out var mappedIcon))
        {
            return mappedIcon;
        }

        if (SupportedIcons.Contains(trimmed))
        {
            return trimmed;
        }

        return defaultIconKey;
    }
}
