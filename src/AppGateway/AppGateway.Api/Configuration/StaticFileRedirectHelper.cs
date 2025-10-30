using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.FileProviders;

namespace AppGateway.Api.Configuration;

internal static class StaticFileRedirectHelper
{
    public static string? ResolveDirectoryRedirect(
        PathString requestPath,
        PathString requestPathBase,
        QueryString queryString,
        string uiRequestPath,
        IFileProvider fileProvider)
    {
        ArgumentNullException.ThrowIfNull(fileProvider);

        if (TryResolveDirectory(requestPath, fileProvider, out var normalizedPath))
        {
            return UriHelper.BuildRelative(null, requestPathBase, normalizedPath, queryString);
        }

        if (!string.IsNullOrWhiteSpace(uiRequestPath))
        {
            var uiPath = new PathString(uiRequestPath);

            if (requestPath.StartsWithSegments(uiPath, out var remainder) && remainder.HasValue)
            {
                if (TryResolveDirectory(remainder, fileProvider, out var normalizedRemainder))
                {
                    var combined = uiPath.Add(normalizedRemainder);
                    return UriHelper.BuildRelative(null, requestPathBase, combined, queryString);
                }
            }
        }

        return null;
    }

    private static bool TryResolveDirectory(PathString path, IFileProvider fileProvider, out PathString normalizedPath)
    {
        normalizedPath = default;

        if (!path.HasValue)
        {
            return false;
        }

        var value = path.Value!;

        if (string.IsNullOrEmpty(value) || string.Equals(value, "/", StringComparison.Ordinal) || value.EndsWith('/'))
        {
            return false;
        }

        if (Path.HasExtension(value))
        {
            return false;
        }

        var relativePath = value.TrimStart('/');

        if (relativePath.Length == 0)
        {
            return false;
        }

        var directoryContents = fileProvider.GetDirectoryContents(relativePath);

        if (!directoryContents.Exists)
        {
            return false;
        }

        normalizedPath = path.Add(new PathString("/"));
        return true;
    }
}

