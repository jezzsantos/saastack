using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.AuthTokens;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class AuthTokensRootSpec
{
    private readonly AuthTokensRoot _authTokens;

    public AuthTokensRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());

        _authTokens = AuthTokensRoot.Create(recorder.Object, idFactory.Object, "auserid".ToId()).Value;
    }

    [Fact]
    public void WhenConstructed_ThenInitialized()
    {
        _authTokens.AccessToken.Should().BeNone();
        _authTokens.RefreshToken.Should().BeNone();
        _authTokens.AccessTokenExpiresOn.Should().BeNone();
    }

    [Fact]
    public void WhenSetTokens_ThenSetsTokens()
    {
        var accessTokenExpiresOn = DateTime.UtcNow.AddMinutes(1);
        var refreshTokenExpiresOn = DateTime.UtcNow.AddMinutes(2);

        _authTokens.SetTokens("anaccesstoken", "arefreshtoken", accessTokenExpiresOn, refreshTokenExpiresOn);

        _authTokens.AccessToken.Should().BeSome("anaccesstoken");
        _authTokens.RefreshToken.Should().BeSome("arefreshtoken");
        _authTokens.AccessTokenExpiresOn.Should().BeSome(accessTokenExpiresOn);
        _authTokens.RefreshTokenExpiresOn.Should().BeSome(refreshTokenExpiresOn);
        _authTokens.Events.Last().Should().BeOfType<TokensChanged>();
    }

    [Fact]
    public void WhenRenewTokensAndOldTokenNotMatch_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        _authTokens.SetTokens("anaccesstoken1", "arefreshtoken1", expiresOn, expiresOn);

        var result = _authTokens.RenewTokens("anotherrefreshtoken", "anaccesstoken2", "arefreshtoken2", expiresOn,
            expiresOn);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.AuthTokensRoot_RefreshTokenNotMatched);
    }

    [Fact]
    public void WhenRenewTokensAndRevoked_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        _authTokens.SetTokens("anaccesstoken1", "arefreshtoken1", expiresOn, expiresOn);
        _authTokens.Revoke("arefreshtoken1");

        var result =
            _authTokens.RenewTokens("arefreshtoken1", "anaccesstoken2", "arefreshtoken2", expiresOn, expiresOn);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.AuthTokensRoot_TokensRevoked);
    }

    [Fact]
    public void WhenRenewTokensAndOldTokenIsExpired_ThenReturnsError()
    {
        var accessTokenExpiresOn1 = DateTime.UtcNow.AddMinutes(1);
        var refreshTokenExpiresOn1 = DateTime.UtcNow.SubtractSeconds(1);
        var accessTokenExpiresOn2 = DateTime.UtcNow.AddMinutes(2);
        var refreshTokenExpiresOn2 = accessTokenExpiresOn1.AddMinutes(2);
#if TESTINGONLY
        _authTokens.TestingOnly_SetTokens("anaccesstoken1", "arefreshtoken1", accessTokenExpiresOn1,
            refreshTokenExpiresOn1);
#endif
        var result = _authTokens.RenewTokens("arefreshtoken1", "anaccesstoken2", "arefreshtoken2",
            accessTokenExpiresOn2, refreshTokenExpiresOn2);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.AuthTokensRoot_RefreshTokenExpired);
    }

    [Fact]
    public void WhenRenewTokens_ThenUpdatesTokens()
    {
        var accessTokenExpiresOn1 = DateTime.UtcNow.AddMinutes(1);
        var refreshTokenExpiresOn1 = DateTime.UtcNow.AddMinutes(1);
        var accessTokenExpiresOn2 = accessTokenExpiresOn1.AddMinutes(2);
        var refreshTokenExpiresOn2 = accessTokenExpiresOn1.AddMinutes(2);
        _authTokens.SetTokens("anaccesstoken1", "arefreshtoken1", accessTokenExpiresOn1, refreshTokenExpiresOn1);

        _authTokens.RenewTokens("arefreshtoken1", "anaccesstoken2", "arefreshtoken2", accessTokenExpiresOn2,
            refreshTokenExpiresOn2);

        _authTokens.AccessToken.Should().BeSome("anaccesstoken2");
        _authTokens.RefreshToken.Should().BeSome("arefreshtoken2");
        _authTokens.AccessTokenExpiresOn.Should().BeSome(accessTokenExpiresOn2);
        _authTokens.RefreshTokenExpiresOn.Should().BeSome(refreshTokenExpiresOn2);
        _authTokens.Events.Last().Should().BeOfType<TokensRefreshed>();
    }

    [Fact]
    public void WhenRevokeAndRevoked_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        _authTokens.SetTokens("anaccesstoken1", "arefreshtoken1", expiresOn, expiresOn);
        _authTokens.Revoke("arefreshtoken1");

        var result = _authTokens.Revoke("arefreshtoken1");

        result.Should().BeError(ErrorCode.RuleViolation, Resources.AuthTokensRoot_TokensRevoked);
    }

    [Fact]
    public void WhenRevokeAndOldTokenNotMatched_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        _authTokens.SetTokens("anaccesstoken1", "arefreshtoken1", expiresOn, expiresOn);

        var result = _authTokens.Revoke("anotherrefreshtoken");

        result.Should().BeError(ErrorCode.RuleViolation, Resources.AuthTokensRoot_RefreshTokenNotMatched);
    }

    [Fact]
    public void WhenRevoke_ThenDeletesTokens()
    {
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        _authTokens.SetTokens("anaccesstoken", "arefreshtoken", expiresOn, expiresOn);

        _authTokens.Revoke("arefreshtoken");

        _authTokens.AccessToken.Should().BeNone();
        _authTokens.RefreshToken.Should().BeNone();
        _authTokens.AccessTokenExpiresOn.Should().BeNone();
        _authTokens.RefreshTokenExpiresOn.Should().BeNone();
        _authTokens.Events.Last().Should().BeOfType<TokensRevoked>();
    }
}