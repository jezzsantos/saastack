using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.SSOUsers;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;

namespace IdentityDomain;

public sealed class SSOAuthTokens : SingleValueObjectBase<SSOAuthTokens, List<SSOAuthToken>>
{
    public static Result<SSOAuthTokens, Error> Create(List<SSOToken> tokens)
    {
        var list = new List<SSOAuthToken>();
        foreach (var token in tokens)
        {
            var tok = SSOAuthToken.Create(token.Type.ToEnumOrDefault(SSOAuthTokenType.AccessToken),
                token.EncryptedValue,
                token.ExpiresOn);
            if (tok.IsFailure)
            {
                return tok.Error;
            }

            list.Add(tok.Value);
        }

        return Create(list);
    }

    public static Result<SSOAuthTokens, Error> Create(List<SSOAuthToken> value)
    {
        return new SSOAuthTokens(value);
    }

    private SSOAuthTokens(List<SSOAuthToken> value) : base(value)
    {
    }

    public static ValueObjectFactory<SSOAuthTokens> Rehydrate()
    {
        return (property, container) =>
        {
            var items = RehydrateToList(property, true, true);
            return new SSOAuthTokens(items.Select(item => SSOAuthToken.Rehydrate()(item!, container)).ToList());
        };
    }

    [SkipImmutabilityCheck]
    public List<SSOAuthToken> ToList()
    {
        return Value;
    }
}