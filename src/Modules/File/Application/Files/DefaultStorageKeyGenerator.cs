using System;
using System.Globalization;
using System.IO;

namespace ECM.File.Application.Files;

internal sealed class DefaultStorageKeyGenerator : IStorageKeyGenerator
{
    public string Generate(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        var extension = Path.GetExtension(fileName);
        var normalizedExtension = string.IsNullOrWhiteSpace(extension)
            ? string.Empty
            : extension.ToLower(CultureInfo.InvariantCulture);

        return $"{Guid.NewGuid():N}{normalizedExtension}";
    }
}
