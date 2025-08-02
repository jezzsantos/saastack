using System.Collections;
using Common;
using IdentityDomain.DomainServices;

namespace IdentityDomain;

public sealed class OAuth2ClientSecrets : IReadOnlyList<OAuth2ClientSecret>
{
    private readonly List<OAuth2ClientSecret> _clientSecrets = new();

    public Result<Error> EnsureInvariants()
    {
        return Result.Ok;
    }

    public int Count => _clientSecrets.Count;

    public IEnumerator<OAuth2ClientSecret> GetEnumerator()
    {
        return _clientSecrets.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public OAuth2ClientSecret this[int index] => _clientSecrets[index];

    public void Add(OAuth2ClientSecret secret)
    {
        _clientSecrets.Add(secret);
    }

    public Result<Error> Verify(IPasswordHasherService passwordHasherService, string secret)
    {
        foreach (var clientSecret in _clientSecrets)
        {
            if (clientSecret.IsMatch(passwordHasherService, secret))
            {
                return clientSecret.IsExpired
                    ? Error.RuleViolation(Resources.OAuth2ClientSecrets_SecretExpired)
                    : Result.Ok;
            }
        }

        return Error.EntityNotFound(Resources.OAuth2ClientSecrets_UnknownSecret);
    }
}