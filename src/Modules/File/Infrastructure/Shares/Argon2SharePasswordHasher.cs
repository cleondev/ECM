using System.Security.Cryptography;
using System.Text;
using ECM.File.Application.Shares;
using Isopoh.Cryptography.Argon2;

namespace ECM.File.Infrastructure.Shares;

public sealed class Argon2SharePasswordHasher : ISharePasswordHasher
{
    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var passwordBytes = Encoding.UTF8.GetBytes(password);
        var salt = RandomNumberGenerator.GetBytes(16);

        var config = new Argon2Config
        {
            Type = Argon2Type.Id,
            TimeCost = 4,
            MemoryCost = 1 << 16,
            Lanes = 2,
            Threads = Environment.ProcessorCount > 0 ? Math.Min(Environment.ProcessorCount, 4) : 2,
            Password = passwordBytes,
            Salt = salt,
        };

        return Argon2.Hash(config);
    }

    public bool Verify(string password, string hash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);

        return Argon2.Verify(hash, password);
    }
}
