using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using Domain.Services.Shared;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class SSOAuthToken : ValueObjectBase<SSOAuthToken>
{
    public static Result<SSOAuthToken, Error> Create(SSOAuthTokenType type, string value, DateTime? expiresOn,
        IEncryptionService encryptionService)
    {
        if (value.IsNotValuedParameter(nameof(value), out var error1))
        {
            return error1;
        }

        var encrypted = encryptionService.Encrypt(value);
        return Create(type, encrypted, expiresOn);
    }

    public static Result<SSOAuthToken, Error> Create(SSOAuthTokenType type, string encryptedValue, DateTime? expiresOn)
    {
        if (encryptedValue.IsNotValuedParameter(nameof(encryptedValue), out var error1))
        {
            return error1;
        }

        return new SSOAuthToken(type, encryptedValue, expiresOn);
    }

    private SSOAuthToken(SSOAuthTokenType type, string encryptedValue, DateTime? expiresOn)
    {
        Type = type;
        EncryptedValue = encryptedValue;
        ExpiresOn = expiresOn;
    }

    public string EncryptedValue { get; }

    public DateTime? ExpiresOn { get; }

    public SSOAuthTokenType Type { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<SSOAuthToken> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new SSOAuthToken(parts[0].ToEnumOrDefault(SSOAuthTokenType.AccessToken), parts[1]!,
                parts[2]?.FromIso8601());
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object?[] { Type, EncryptedValue, ExpiresOn };
    }

    [SkipImmutabilityCheck]
    public string GetValue(IEncryptionService encryptionService)
    {
        return encryptionService.Decrypt(EncryptedValue);
    }
}