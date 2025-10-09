using System;
using ECM.BuildingBlocks.Application;
using ECM.Modules.File.Domain.Files;

namespace ECM.Modules.File.Application.Files;

public sealed record RegisterFileCommand(string FileName, string ContentType, long Size, string StorageKey)
{
    public OperationResult<FileEntry> ToDomain()
    {
        if (Size <= 0)
        {
            return OperationResult<FileEntry>.Failure("File size must be greater than zero");
        }

        var metadata = new FileMetadata(FileName, ContentType, Size);
        var entry = new FileEntry(FileId.New(), metadata, StorageKey, DateTimeOffset.UtcNow);
        return OperationResult<FileEntry>.Success(entry);
    }
}
