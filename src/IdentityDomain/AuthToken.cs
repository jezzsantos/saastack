using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using Domain.Services.Shared;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class AuthToken : ValueObjectBase<AuthToken>
{
    public static Result<AuthToken, Error> Create(AuthTokenType type, string plainValue, DateTime? expiresOn,
        IEncryptionService encryptionService)
    {
        if (plainValue.IsNotValuedParameter(nameof(plainValue), out var error1))
        {
            return error1;
        }

        //Ignore refresh tokens, they are not base64encoded tokens
        if (type != AuthTokenType.RefreshToken)
        {
            if (plainValue.IsInvalidParameter(IsValidPlainTokenValue, nameof(plainValue), out _))
            {
                return Error.Validation(Resources.AuthToken_InvalidPlainValue);
            }
        }

        var encrypted = encryptionService.Encrypt(plainValue);
        return Create(type, encrypted, expiresOn);
    }

    public static Result<AuthToken, Error> Create(AuthTokenType type, string encryptedValue, DateTime? expiresOn)
    {
        if (encryptedValue.IsNotValuedParameter(nameof(encryptedValue), out var error1))
        {
            return error1;
        }

        if (encryptedValue.IsInvalidParameter(s => !IsValidPlainTokenValue(s), nameof(encryptedValue), out _))
        {
            return Error.Validation(Resources.AuthToken_InvalidEncryptedValue);
        }

        return new AuthToken(type, encryptedValue, expiresOn.ToOptional());
    }

    public static Result<AuthToken, Error> Create(Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken token)
    {
        return Create(token.Type.ToEnumOrDefault(AuthTokenType.OtherToken), token.EncryptedValue, token.ExpiresOn);
    }

    private AuthToken(AuthTokenType type, string encryptedValue, Optional<DateTime> expiresOn)
    {
        Type = type;
        EncryptedValue = encryptedValue;
        ExpiresOn = expiresOn;
    }

    public string EncryptedValue { get; }

    public Optional<DateTime> ExpiresOn { get; }

    public AuthTokenType Type { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<AuthToken> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new AuthToken(
                parts[0].Value.ToEnumOrDefault(AuthTokenType.AccessToken),
                parts[1],
                parts[2].ToOptional(val => val.FromIso8601()));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Type, EncryptedValue, ExpiresOn];
    }

    [SkipImmutabilityCheck]
    public string GetDecryptedValue(IEncryptionService encryptionService)
    {
        return encryptionService.Decrypt(EncryptedValue);
    }

    private static bool IsValidPlainTokenValue(string token)
    {
        return token.StartsWith("eyJ");
    }
}