using Common;
using Domain.Interfaces;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class OAuth2ScopesSpec
{
    [Fact]
    public void WhenCreateWithEmptyList_ThenReturnsEmpty()
    {
        var result = OAuth2Scopes.Create([]);

        result.Should().BeSuccess();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreateWithInvalidScope_ThenReturnsError()
    {
        var result = OAuth2Scopes.Create(["aninvalidscope"]);

        result.Should().BeError(ErrorCode.Validation, Resources.OAuth2Scopes_InvalidScope);
    }

    [Fact]
    public void WhenCreateWithListOfScope_ThenReturns()
    {
        var result = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainSingle(OAuth2Constants.Scopes.Profile);
    }

    [Fact]
    public void WhenCreateWithEmptyString_ThenReturnsEmpty()
    {
        var result = OAuth2Scopes.Create(string.Empty);

        result.Should().BeSuccess();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreateWithInvalidScopeString_ThenReturnsError()
    {
        var result = OAuth2Scopes.Create("aninvalidscope");

        result.Should().BeError(ErrorCode.Validation, Resources.OAuth2Scopes_InvalidScope);
    }

    [Fact]
    public void WhenCreateWithScope_ThenReturns()
    {
        var result = OAuth2Scopes.Create(OAuth2Constants.Scopes.Profile);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainSingle(OAuth2Constants.Scopes.Profile);
    }

    [Fact]
    public void WhenHasAndNoScopes_ThenReturnsFalse()
    {
        var scopes = OAuth2Scopes.Create([]).Value;

        var result = scopes.Has(OpenIdConnectConstants.Scopes.OpenId);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasWithDifferentScope_ThenReturnsFalse()
    {
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]).Value;

        var result = scopes.Has(OAuth2Constants.Scopes.Email);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasWithSameScope_ThenReturnsTrue()
    {
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]).Value;

        var result = scopes.Has(OAuth2Constants.Scopes.Profile);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasWithExistingScope_ThenReturnsTrue()
    {
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId, OAuth2Constants.Scopes.Profile]).Value;

        var result = scopes.Has(OAuth2Constants.Scopes.Profile);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasAllAndNoScopes_ThenReturnsFalse()
    {
        var scopes = OAuth2Scopes.Create([]).Value;

        var result = scopes.HasAll(OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]).Value);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasAllAndDifferentScopes_ThenReturnsFalse()
    {
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]).Value;

        var result = scopes.HasAll(OAuth2Scopes.Create([OAuth2Constants.Scopes.Email]).Value);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasAllAndSomeMatchingScopes_ThenReturnsFalse()
    {
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile, OAuth2Constants.Scopes.Email]).Value;

        var result =
            scopes.HasAll(OAuth2Scopes.Create([OAuth2Constants.Scopes.Email, OAuth2Constants.Scopes.Phone]).Value);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasAllAndAllMatchingScopes_ThenReturnsTrue()
    {
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile, OAuth2Constants.Scopes.Email]).Value;

        var result = scopes.HasAll(OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile, OAuth2Constants.Scopes.Email])
            .Value);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasAllAndSubsetOfAllScopes_ThenReturnsTrue()
    {
        var scopes = OAuth2Scopes.Create([
            OAuth2Constants.Scopes.Profile, OAuth2Constants.Scopes.Email, OAuth2Constants.Scopes.Phone
        ]).Value;

        var result = scopes.HasAll(OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile, OAuth2Constants.Scopes.Email])
            .Value);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsSubsetOfAndBothEmpty_ThenReturnsTrue()
    {
        var scopes = OAuth2Scopes.Create([]).Value;

        var result = OAuth2Scopes.Create([]).Value
            .IsSubsetOf(scopes);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsSubsetOfAndBothDifferent_ThenReturnsFalse()
    {
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]).Value;

        var result = OAuth2Scopes.Create([OAuth2Constants.Scopes.Email]).Value
            .IsSubsetOf(scopes);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsSubsetOfAndONlyIncludesSingle_ThenReturnsFalse()
    {
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]).Value;

        var result = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile, OAuth2Constants.Scopes.Email]).Value
            .IsSubsetOf(scopes);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsSubsetOfAndAllIncluded_ThenReturnsTrue()
    {
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile, OAuth2Constants.Scopes.Email]).Value;

        var result = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]).Value
            .IsSubsetOf(scopes);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenIsSubsetOfAndBothExistInBoth_ThenReturnsTrue()
    {
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile, OAuth2Constants.Scopes.Email]).Value;

        var result = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile, OAuth2Constants.Scopes.Email]).Value
            .IsSubsetOf(scopes);

        result.Should().BeTrue();
    }
}