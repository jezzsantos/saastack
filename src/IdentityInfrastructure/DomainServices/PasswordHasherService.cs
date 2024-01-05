using BCrypt.Net;
using Domain.Interfaces.Validations;
using IdentityDomain.DomainServices;

namespace IdentityInfrastructure.DomainServices;

public class PasswordHasherService : IPasswordHasherService
{
    public string HashPassword(string password)
    {
        var salt = BCrypt.Net.BCrypt.GenerateSalt(12);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password, salt);
        return passwordHash;
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, passwordHash);
        }
        catch (SaltParseException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public bool ValidatePasswordHash(string passwordHash)
    {
        return CommonValidations.Passwords.PasswordHash.Matches(passwordHash);
    }

    public bool ValidatePassword(string password, bool isStrict)
    {
        return isStrict
            ? CommonValidations.Passwords.Password.Strict.Matches(password)
            : CommonValidations.Passwords.Password.Loose.Matches(password);
    }
}