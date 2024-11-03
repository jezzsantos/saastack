using Domain.Common.ValueObjects;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class MfaCallerSpec
{
    [Fact]
    public void WhenCreateWithNoAuthenticationToken_ThenCreates()
    {
        var result = MfaCaller.Create("acallerid".ToId(), null);

        result.Should().BeSuccess();
        result.Value.AuthenticationToken.Should().BeNone();
        result.Value.CallerId.Should().Be("acallerid".ToId());
        result.Value.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void WhenCreateWithAuthenticationToken_ThenCreates()
    {
        var result = MfaCaller.Create("acallerid".ToId(), "atoken");

        result.Should().BeSuccess();
        result.Value.AuthenticationToken.Should().BeSome("atoken");
        result.Value.CallerId.Should().Be("acallerid".ToId());
        result.Value.IsAuthenticated.Should().BeFalse();
    }
}