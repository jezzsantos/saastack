using FluentAssertions;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class VerificationSpec
{
    [Fact]
    public void WhenConstructed_ThenIsNotSet()
    {
        var invitation = Verification.Create().Value;

        invitation.IsStillVerifying.Should().BeFalse();
    }

    [Fact]
    public void WhenIsStillValidAndNoToken_ThenReturnsFalse()
    {
        var invitation = Verification.Create().Value;

        invitation.IsStillVerifying.Should().BeFalse();
        invitation.Token.Should().BeNone();
        invitation.ExpiresUtc.Should().BeNone();
    }

    [Fact]
    public void WhenIsStillValidAfterSet_ThenReturnsTrue()
    {
        var invitation = Verification.Create().Value;
        invitation = invitation.Renew("atoken");

        invitation.IsStillVerifying.Should().BeTrue();
        ((object)invitation.Token).Should().Be("atoken");
        invitation.ExpiresUtc.Value.Should().BeNear(DateTime.UtcNow.Add(Verification.DefaultTokenExpiry));
    }
}