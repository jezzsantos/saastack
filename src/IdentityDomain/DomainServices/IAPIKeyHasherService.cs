namespace IdentityDomain.DomainServices;

public interface IAPIKeyHasherService
{
    string HashAPIKey(string key);

    bool ValidateAPIKeyHash(string keyHash);

    bool ValidateKey(string key);

    bool VerifyAPIKey(string key, string keyHash);
}