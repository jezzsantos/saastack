using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using IdentityDomain.DomainServices;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class OAuth2ClientSecret : ValueObjectBase<OAuth2ClientSecret>
{
    public static Result<OAuth2ClientSecret, Error> Create(string secretHash, string firstFour,
        Optional<DateTime> expiresOn)
    {
        if (secretHash.IsNotValuedParameter(nameof(secretHash), Resources.OAuth2ClientSecret_InvalidSecretHash,
                out var error1))
        {
            return error1;
        }

        if (firstFour.IsInvalidParameter(Validations.OAuth2.ClientSecretFirstFour, nameof(firstFour),
                Resources.OAuth2ClientSecret_InvalidFirstFour, out var error2))
        {
            return error2;
        }

        return new OAuth2ClientSecret(secretHash, firstFour, expiresOn);
    }

    public static Result<OAuth2ClientSecret, Error> Create(string secret, Optional<DateTime> expiresOn,
        IPasswordHasherService passwordHasherService)
    {
        if (secret.IsInvalidParameter(Validations.OAuth2.ClientSecret, nameof(secret),
                Resources.OAuth2ClientSecret_InvalidSecret, out var error1))
        {
            return error1;
        }

        var firstFour = secret.Substring(0, 4);
        var secretHash = passwordHasherService.HashPassword(secret);
        return new OAuth2ClientSecret(secretHash, firstFour, expiresOn);
    }

    private OAuth2ClientSecret(string secretHash, string firstFour, Optional<DateTime> expiresOn)
    {
        SecretHash = secretHash;
        ExpiresOn = expiresOn;
        FirstFour = firstFour;
    }

    public Optional<DateTime> ExpiresOn { get; }

    public string FirstFour { get; }

    public bool IsExpired => ExpiresOn.HasValue && ExpiresOn.Value < DateTime.UtcNow;

    public string SecretHash { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<OAuth2ClientSecret> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new OAuth2ClientSecret(
                parts[0],
                parts[1],
                parts[2].ToOptional(val => val.FromIso8601()));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [SecretHash, FirstFour, ExpiresOn];
    }

    [SkipImmutabilityCheck]
    public bool IsMatch(IPasswordHasherService passwordHasherService, string secret)
    {
        return passwordHasherService.VerifyPassword(secret, SecretHash);
    }
}