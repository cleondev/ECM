using System;

namespace ECM.Modules.File.Domain.Files;

public readonly record struct FileId(Guid Value)
{
    public static FileId New() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();
}
