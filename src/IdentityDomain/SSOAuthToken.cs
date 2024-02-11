using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace IdentityDomain;

public sealed class SSOAuthToken : ValueObjectBase<SSOAuthToken>
{
    public static Result<SSOAuthToken, Error> Create(SSOAuthTokenType type, string value, DateTime? expiresOn)
    {
        if (value.IsNotValuedParameter(nameof(value), out var error1))
        {
            return error1;
        }

        return new SSOAuthToken(type, value, expiresOn);
    }

    private SSOAuthToken(SSOAuthTokenType type, string value, DateTime? expiresOn)
    {
        Type = type;
        Value = value;
        ExpiresOn = expiresOn;
    }

    public DateTime? ExpiresOn { get; }

    public SSOAuthTokenType Type { get; }

    public string Value { get; }

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
        return new object?[] { Type, Value, ExpiresOn };
    }
}