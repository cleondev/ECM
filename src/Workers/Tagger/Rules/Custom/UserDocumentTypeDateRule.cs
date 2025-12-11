using System;
using System.Collections.Generic;

using Ecm.Rules.Abstractions;

using Tagger.Rules.Configuration;

namespace Tagger.Rules.Custom;

/// <summary>
/// Creates a user-scoped tag that combines the upload date and document type.
/// </summary>
internal sealed class UserDocumentTypeDateRule : IRule
{
    private static readonly string[] UserIdMetadataKeys =
    {
        "uploadedByUserId",
        "uploadedBy",
        "createdBy",
        "ownerUserId",
        "userId",
    };

    public string Name => "User Document Type Date";

    public bool Match(IRuleContext ctx)
    {
        return ctx.Get("OccurredAtUtc", default(DateTimeOffset)) != default
            && DocumentTypeRule.TryResolveExtension(ctx, out _);
    }

    public void Apply(IRuleContext ctx, IRuleOutput output)
    {
        var occurredAtUtc = ctx.Get("OccurredAtUtc", default(DateTimeOffset));
        if (occurredAtUtc == default)
        {
            return;
        }

        if (!DocumentTypeRule.TryResolveExtension(ctx, out var extension))
        {
            return;
        }

        var extensionToken = extension.TrimStart('.').ToUpperInvariant();
        var tagName = $"{occurredAtUtc:dd-MM-yyyy}-{extensionToken}";

        output.AddTag(
            TagDefinition.Create(
                tagName,
                TagDefaults.DefaultPathSegments,
                scope: TagScope.User,
                ownerUserId: ResolveOwnerUserId(ctx),
                namespaceDisplayName: "User Uploads"));
    }

    private static Guid? ResolveOwnerUserId(IRuleContext ctx)
    {
        var metadata = ctx.Get("Metadata", default(IDictionary<string, string>));
        if (metadata is not null)
        {
            foreach (var key in UserIdMetadataKeys)
            {
                if (!metadata.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (Guid.TryParse(value, out var userId))
                {
                    return userId;
                }
            }
        }

        foreach (var key in UserIdMetadataKeys)
        {
            var value = ctx.Get(key, default(string));
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (Guid.TryParse(value, out var userId))
            {
                return userId;
            }
        }

        return null;
    }
}
