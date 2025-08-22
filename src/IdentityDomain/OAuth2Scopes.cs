using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace IdentityDomain;

public sealed class OAuth2Scopes : SingleValueObjectBase<OAuth2Scopes, List<string>>
{
    public static readonly OAuth2Scopes Empty = new([]);
    private static readonly char[] ScopeDelimiters = Validations.OAuth2.Delimiters;

    public static Result<OAuth2Scopes, Error> Create(List<string> scopes)
    {
        if (scopes.HasAny())
        {
            foreach (var scope in scopes)
            {
                if (scope.IsInvalidParameter(s => OpenIdConnectConstants.Scopes.AllScopes.Contains(s), nameof(scopes),
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
            return Create([]);
        }

        var scopes = scope
            .Split(ScopeDelimiters, StringSplitOptions.RemoveEmptyEntries)
            .ToList();
        return Create(scopes);
    }

    private OAuth2Scopes(List<string> value) : base(value)
    {
    }

    public bool HasNone => Items.Count == 0;

    public List<string> Items => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<OAuth2Scopes> Rehydrate()
    {
        return (property, _) =>
        {
            var items = RehydrateToList(property, true, true);
            return new OAuth2Scopes(
                items
                    .Where(item => item.HasValue)
                    .Select(item => item.Value)
                    .ToList());
        };
    }

    [SkipImmutabilityCheck]
    public bool Has(string scope)
    {
        return Value.Contains(scope);
    }

    [SkipImmutabilityCheck]
    public bool HasAll(OAuth2Scopes scopes)
    {
        return scopes.Value.All(Has);
    }

    [SkipImmutabilityCheck]
    public bool IsSubsetOf(OAuth2Scopes scopes)
    {
        return Items.All(scopes.Has);
    }
}