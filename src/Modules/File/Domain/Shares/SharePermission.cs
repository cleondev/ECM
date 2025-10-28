namespace ECM.File.Domain.Shares;

[Flags]
public enum SharePermission
{
    None = 0,
    View = 1 << 0,
    Download = 1 << 1,
}
