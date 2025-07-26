using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class OAuth2Scopes : SingleValueObjectBase<OAuth2Scopes, List<string>>
{
    public static readonly OAuth2Scopes Empty = new([]);
    private static readonly char[] ScopeDelimiters = [' ', ';', ','];

    public static Result<OAuth2Scopes, Error> Create(List<string> scopes)
    {
        if (scopes.HasAny())
        {
            foreach (var scope in scopes)
            {
                if (scope.IsInvalidParameter(s => OAuth2Constants.Scopes.AllScopes.Contains(s), nameof(scopes),
                        Resources.OAuth2Scopes_InvalidScope, out var error))
                {
                    return error;
                }
            }
        }

        return new OAuth2Scopes(scopes);
    }

    public static Result<OAuth2Scopes, Error> Create(string? scope)
    {
        if (scope.HasNoValue())
        {
            return Create(OpenIdConnectConstants.Scopes.Default.ToList());
        }

        var scopes = scope
            .Split(ScopeDelimiters, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        return Create(scopes);
    }

    private OAuth2Scopes(List<string> value) : base(value)
    {
    }

    public List<string> Items => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<OAuth2Scopes> Rehydrate()
    {
        return (property, _) =>
        {
            var items = RehydrateToList(property, true, true);
            return new OAuth2Scopes(
                items.Select(item => item)
                    .Where(item => item.Exists())
                    .ToList()!);
        };
    }
}