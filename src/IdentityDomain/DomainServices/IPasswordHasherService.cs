namespace IdentityDomain.DomainServices;

public interface IPasswordHasherService
{
    string HashPassword(string password);

    bool ValidatePassword(string password, bool isStrict);

    bool ValidatePasswordHash(string passwordHash);

    bool VerifyPassword(string password, string passwordHash);
}