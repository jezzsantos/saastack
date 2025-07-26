using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class OAuth2ClientSecret : ValueObjectBase<OAuth2ClientSecret>
{
    public static Result<OAuth2ClientSecret, Error> Create(string secretHash, Optional<DateTime> expiresOn)
    {
        if (secretHash.IsNotValuedParameter(nameof(secretHash), out var error1))
        {
            return error1;
        }

        return new OAuth2ClientSecret(secretHash, expiresOn);
    }

    private OAuth2ClientSecret(string secretHash, Optional<DateTime> expiresOn)
    {
        SecretHash = secretHash;
        ExpiresOn = expiresOn;
    }

    public Optional<DateTime> ExpiresOn { get; }

    public string SecretHash { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<OAuth2ClientSecret> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new OAuth2ClientSecret(parts[0]!, parts[1].FromValueOrNone(val => val.FromIso8601()));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { SecretHash, ExpiresOn };
    }
}