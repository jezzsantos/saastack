using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class SSOAuthTokenSpec
{
    [Fact]
    public void WhenCreateAndValueIsEmpty_ThenReturnsError()
    {
        var result = SSOAuthToken.Create(SSOAuthTokenType.AccessToken, string.Empty, null);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreate_ThenReturns()
    {
        var expiresOn = DateTime.UtcNow;
        var result = SSOAuthToken.Create(SSOAuthTokenType.AccessToken, "atoken", expiresOn);

        result.Should().BeSuccess();
        result.Value.Type.Should().Be(SSOAuthTokenType.AccessToken);
        result.Value.Value.Should().Be("atoken");
        result.Value.ExpiresOn.Should().Be(expiresOn);
    }
}