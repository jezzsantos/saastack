using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Services.Shared;
using JetBrains.Annotations;

namespace IdentityDomain;

/// <summary>
///     A lightweight auth token memento that can be used to search for tokens, without having to persist the full token
///     value, which is never needed
/// </summary>
public sealed class OAuth2TokenMemento : ValueObjectBase<OAuth2TokenMemento>
{
    public static Result<OAuth2TokenMemento, Error> Create(AuthTokenType type, string plainValue, DateTime? expiresOn,
        ITokensService tokensService)
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
                return Error.Validation(Resources.OAuth2TokenMemento_InvalidPlainValue);
            }
        }

        var digest = tokensService.CreateTokenDigest(plainValue);
        return Create(type, digest, expiresOn);
    }

    public static Result<OAuth2TokenMemento, Error> Create(AuthTokenType type, string digestValue, DateTime? expiresOn)
    {
        if (digestValue.IsNotValuedParameter(nameof(digestValue), out var error1))
        {
            return error1;
        }

        if (digestValue.IsInvalidParameter(s => !IsValidPlainTokenValue(s), nameof(digestValue), out _))
        {
            return Error.Validation(Resources.OAuth2TokenMemento_InvalidDigestValue);
        }

        return new OAuth2TokenMemento(type, digestValue, expiresOn.ToOptional());
    }

    public static Result<OAuth2TokenMemento, Error> Create(AuthToken token,
        IEncryptionService encryptionService, ITokensService tokensService)
    {
        var plainValue = token.GetDecryptedValue(encryptionService);
        var digest = tokensService.CreateTokenDigest(plainValue);

        return Create(token.Type, digest, token.ExpiresOn);
    }

    private OAuth2TokenMemento(AuthTokenType type, string digestValue, Optional<DateTime> expiresOn)
    {
        Type = type;
        DigestValue = digestValue;
        ExpiresOn = expiresOn;
    }

    public string DigestValue { get; }

    public Optional<DateTime> ExpiresOn { get; }

    public AuthTokenType Type { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<OAuth2TokenMemento> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new OAuth2TokenMemento(parts[0].ToEnumOrDefault(AuthTokenType.AccessToken), parts[1]!,
                parts[2].ToOptional<string?, DateTime>(val => val.FromIso8601()));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Type, ExpiresOn, DigestValue];
    }

    private static bool IsValidPlainTokenValue(string token)
    {
        return token.StartsWith("eyJ");
    }
}