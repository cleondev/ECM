using System;

namespace ECM.File.Domain.Files;

public readonly record struct FileId(Guid Value)
{
    public static FileId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
