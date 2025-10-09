using System;

namespace ECM.Modules.File.Domain.Files;

public sealed class FileMetadata
{
    public FileMetadata(string fileName, string contentType, long size)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name cannot be empty", nameof(fileName));
        }

        FileName = fileName;
        ContentType = contentType;
        Size = size;
    }

    public string FileName { get; }

    public string ContentType { get; }

    public long Size { get; }
}
