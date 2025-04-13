using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class AuthTokens : SingleValueObjectBase<AuthTokens, List<AuthToken>>
{
    public static Result<AuthTokens, Error> Create(
        List<Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken> tokens)
    {
        var list = new List<AuthToken>();
        foreach (var token in tokens)
        {
            var tok = AuthToken.Create(token.Type.ToEnumOrDefault(AuthTokenType.AccessToken),
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

    public static Result<AuthTokens, Error> Create(List<AuthToken> value)
    {
        return new AuthTokens(value);
    }

    private AuthTokens(List<AuthToken> value) : base(value)
    {
    }

    [UsedImplicitly]
    public static ValueObjectFactory<AuthTokens> Rehydrate()
    {
        return (property, container) =>
        {
            var items = RehydrateToList(property, true, true);
            return new AuthTokens(items.Select(item => AuthToken.Rehydrate()(item!, container)).ToList());
        };
    }

    [SkipImmutabilityCheck]
    public List<AuthToken> ToList()
    {
        return Value;
    }
}