using System;

namespace ECM.IAM.Domain.Groups;

public static class GroupKindExtensions
{
    public static string ToNormalizedString(this GroupKind kind) => kind switch
    {
        GroupKind.System => "system",
        GroupKind.Unit => "unit",
        GroupKind.Temporary => "temporary",
        GroupKind.Guess => "guess",
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported group kind."),
    };

    public static GroupKind FromString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return GroupKind.Temporary;
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "system" => GroupKind.System,
            "unit" => GroupKind.Unit,
            "temporary" => GroupKind.Temporary,
            "normal" => GroupKind.Temporary,
            "guess" => GroupKind.Guess,
            _ => throw new ArgumentException($"Unknown group kind '{value}'.", nameof(value)),
        };
    }
}
