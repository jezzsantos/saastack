using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;

namespace IdentityDomain;

public sealed class SSOAuthTokens : SingleValueObjectBase<SSOAuthTokens, List<SSOAuthToken>>
{
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
}