using Domain.Interfaces.Validations;
using IdentityDomain.DomainServices;

namespace IdentityInfrastructure.DomainServices;

public class APIKeyHasherService : PasswordHasherService, IAPIKeyHasherService
{
    public string HashAPIKey(string key)
    {
        return HashPassword(key);
    }

    public bool ValidateAPIKeyHash(string keyHash)
    {
        return ValidatePasswordHash(keyHash);
    }

    public bool ValidateKey(string key)
    {
        return CommonValidations.APIKeys.Key.Matches(key);
    }

    public bool VerifyAPIKey(string key, string keyHash)
    {
        return VerifyPassword(key, keyHash);
    }
}