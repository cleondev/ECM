namespace ECM.File.Application.Shares;

public interface ISharePasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
