using System;
using System.IO;

namespace ECM.Abstractions.Files;

public sealed class FileUploadRequest
{
    public FileUploadRequest(string fileName, string contentType, long length, Stream content)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length), length, "File length must be greater than zero.");
        }

        FileName = fileName.Trim();
        ContentType = contentType.Trim();
        Length = length;
        Content = content ?? throw new ArgumentNullException(nameof(content));
    }

    public string FileName { get; }

    public string ContentType { get; }

    public long Length { get; }

    public Stream Content { get; }
}
