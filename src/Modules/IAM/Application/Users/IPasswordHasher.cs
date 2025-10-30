namespace ECM.IAM.Application.Users;

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyHashedPassword(string hashedPassword, string providedPassword);
}
