using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.AuthTokens;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class AuthTokensRootSpec
{
    private readonly AuthTokensRoot _authTokens;
    private readonly Mock<IEncryptionService> _encryptionService;
    private readonly Mock<ITokensService> _tokensService;

    public AuthTokensRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _encryptionService = new Mock<IEncryptionService>();
        _encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
            .Returns("anencryptedvalue");
        _encryptionService.Setup(es => es.Decrypt(It.IsAny<string>()))
            .Returns("adecryptedvalue");
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateTokenDigest(It.IsAny<string>()))
            .Returns("adigestvalue");

        _authTokens = AuthTokensRoot
            .Create(recorder.Object, idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "auserid".ToId()).Value;
    }

    [Fact]
    public void WhenConstructed_ThenInitialized()
    {
        _authTokens.AccessToken.Should().BeNone();
        _authTokens.RefreshToken.Should().BeNone();
        _authTokens.RefreshTokenDigest.Should().BeNone();
        _authTokens.IdToken.Should().BeNone();
        _authTokens.AccessTokenExpiresOn.Should().BeNone();
    }

    [Fact]
    public void WhenSetTokens_ThenSetsTokens()
    {
        var accessTokenExpiresOn = DateTime.UtcNow.AddMinutes(1);
        var refreshTokenExpiresOn = DateTime.UtcNow.AddMinutes(2);
        var idTokenExpiresOn = DateTime.UtcNow.AddMinutes(3);
        var accessToken = AuthToken.Create(AuthTokenType.AccessToken, "anaccesstoken", accessTokenExpiresOn).Value;
        var refreshToken = AuthToken.Create(AuthTokenType.RefreshToken, "arefreshtoken", refreshTokenExpiresOn).Value;
        var idToken = AuthToken.Create(AuthTokenType.OtherToken, "anidtoken", idTokenExpiresOn).Value;

        var result = _authTokens.SetTokens(accessToken, refreshToken, idToken);

        result.Should().BeSuccess();
        _authTokens.AccessToken.Should().BeSome("anaccesstoken");
        _authTokens.RefreshToken.Should().BeSome("arefreshtoken");
        _authTokens.RefreshTokenDigest.Should().BeSome("adigestvalue");
        _authTokens.IdToken.Should().BeSome("anidtoken");
        _authTokens.AccessTokenExpiresOn.Should().BeSome(accessTokenExpiresOn);
        _authTokens.RefreshTokenExpiresOn.Should().BeSome(refreshTokenExpiresOn);
        _authTokens.IdTokenExpiresOn.Should().BeSome(idTokenExpiresOn);
        _authTokens.Events.Last().Should().BeOfType<TokensChanged>();
        _encryptionService.Verify(es => es.Decrypt("arefreshtoken"));
        _tokensService.Verify(ts => ts.CreateTokenDigest("adecryptedvalue"));
    }

    [Fact]
    public void WhenRenewTokensAndOldTokenNotMatch_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        _authTokens.SetTokens(
            AuthToken.Create(AuthTokenType.AccessToken, "anaccesstoken1", expiresOn).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "arefreshtoken1", expiresOn).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anidtoken1", expiresOn).Value);

        var result = _authTokens.RenewTokens("anotherrefreshtoken",
            AuthToken.Create(AuthTokenType.AccessToken, "anaccesstoken2", expiresOn).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "arefreshtoken2", expiresOn).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anidtoken2", expiresOn).Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.AuthTokensRoot_RefreshTokenNotMatched);
        _encryptionService.Verify(es => es.Decrypt("arefreshtoken1"));
    }

    [Fact]
    public void WhenRenewTokensAndRevoked_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        _authTokens.SetTokens(
            AuthToken.Create(AuthTokenType.AccessToken, "anaccesstoken1", expiresOn).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "arefreshtoken1", expiresOn).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anidtoken1", expiresOn).Value);
        _authTokens.Revoke("adecryptedvalue");

        var result =
            _authTokens.RenewTokens("arefreshtoken1",
                AuthToken.Create(AuthTokenType.AccessToken, "anaccesstoken2", expiresOn).Value,
                AuthToken.Create(AuthTokenType.RefreshToken, "arefreshtoken2", expiresOn).Value,
                AuthToken.Create(AuthTokenType.OtherToken, "anidtoken2", expiresOn).Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.AuthTokensRoot_TokensRevoked);
        _encryptionService.Verify(es => es.Decrypt("arefreshtoken1"));
    }

    [Fact]
    public void WhenRenewTokensAndOldTokenIsExpired_ThenReturnsError()
    {
        var accessTokenExpiresOn1 = DateTime.UtcNow.AddMinutes(1);
        var refreshTokenExpiresOn1 = DateTime.UtcNow.SubtractSeconds(1);
        var idTokenExpiresOn1 = DateTime.UtcNow.AddMinutes(1);
        var accessTokenExpiresOn2 = DateTime.UtcNow.AddMinutes(2);
        var refreshTokenExpiresOn2 = accessTokenExpiresOn1.AddMinutes(2);
        var idTokenExpiresOn2 = DateTime.UtcNow.AddMinutes(2);
#if TESTINGONLY
        _authTokens.TestingOnly_SetTokens("anaccesstoken1", "arefreshtoken1", "anidtoken1", accessTokenExpiresOn1,
            refreshTokenExpiresOn1, idTokenExpiresOn1);
#endif
        var result = _authTokens.RenewTokens("adecryptedvalue",
            AuthToken.Create(AuthTokenType.AccessToken, "anaccesstoken2", accessTokenExpiresOn2).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "arefreshtoken2", refreshTokenExpiresOn2).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anidtoken2", idTokenExpiresOn2).Value);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.AuthTokensRoot_RefreshTokenExpired);
        _encryptionService.Verify(es => es.Decrypt("arefreshtoken1"));
    }

    [Fact]
    public void WhenRenewTokens_ThenUpdatesTokens()
    {
        var accessTokenExpiresOn1 = DateTime.UtcNow.AddMinutes(1);
        var refreshTokenExpiresOn1 = DateTime.UtcNow.AddMinutes(1);
        var idTokenExpiresOn1 = DateTime.UtcNow.AddMinutes(1);
        var accessTokenExpiresOn2 = accessTokenExpiresOn1.AddMinutes(2);
        var refreshTokenExpiresOn2 = accessTokenExpiresOn1.AddMinutes(2);
        var idTokenExpiresOn2 = accessTokenExpiresOn1.AddMinutes(2);
        _authTokens.SetTokens(
            AuthToken.Create(AuthTokenType.AccessToken, "anaccesstoken1", accessTokenExpiresOn1).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "arefreshtoken1", refreshTokenExpiresOn1).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anidtoken1", idTokenExpiresOn1).Value);

        var result = _authTokens.RenewTokens("adecryptedvalue",
            AuthToken.Create(AuthTokenType.AccessToken, "anaccesstoken2", accessTokenExpiresOn2).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "arefreshtoken2", refreshTokenExpiresOn2).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anidtoken2", idTokenExpiresOn2).Value);

        result.Should().BeSuccess();
        _authTokens.AccessToken.Should().BeSome("anaccesstoken2");
        _authTokens.RefreshToken.Should().BeSome("arefreshtoken2");
        _authTokens.RefreshTokenDigest.Should().BeSome("adigestvalue");
        _authTokens.IdToken.Should().BeSome("anidtoken2");
        _authTokens.AccessTokenExpiresOn.Should().BeSome(accessTokenExpiresOn2);
        _authTokens.RefreshTokenExpiresOn.Should().BeSome(refreshTokenExpiresOn2);
        _authTokens.IdTokenExpiresOn.Should().BeSome(idTokenExpiresOn2);
        _authTokens.Events.Last().Should().BeOfType<TokensRefreshed>();
    }

    [Fact]
    public void WhenRevokeAndRevoked_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        _authTokens.SetTokens(
            AuthToken.Create(AuthTokenType.AccessToken, "anaccesstoken", expiresOn).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "arefreshtoken", expiresOn).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anidtoken", expiresOn).Value);
        _authTokens.Revoke("adecryptedvalue");

        var result = _authTokens.Revoke("arefreshtoken1");

        result.Should().BeError(ErrorCode.RuleViolation, Resources.AuthTokensRoot_TokensRevoked);
    }

    [Fact]
    public void WhenRevokeAndOldTokenNotMatched_ThenReturnsError()
    {
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        _authTokens.SetTokens(
            AuthToken.Create(AuthTokenType.AccessToken, "anaccesstoken", expiresOn).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "arefreshtoken", expiresOn).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anidtoken", expiresOn).Value);

        var result = _authTokens.Revoke("anotherrefreshtoken");

        result.Should().BeError(ErrorCode.RuleViolation, Resources.AuthTokensRoot_RefreshTokenNotMatched);
    }

    [Fact]
    public void WhenRevoke_ThenDeletesTokens()
    {
        var expiresOn = DateTime.UtcNow.AddMinutes(1);
        _authTokens.SetTokens(
            AuthToken.Create(AuthTokenType.AccessToken, "anaccesstoken", expiresOn).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "arefreshtoken", expiresOn).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anidtoken", expiresOn).Value);

        _authTokens.Revoke("adecryptedvalue");

        _authTokens.AccessToken.Should().BeNone();
        _authTokens.RefreshToken.Should().BeNone();
        _authTokens.RefreshTokenDigest.Should().BeNone();
        _authTokens.IdToken.Should().BeNone();
        _authTokens.AccessTokenExpiresOn.Should().BeNone();
        _authTokens.RefreshTokenExpiresOn.Should().BeNone();
        _authTokens.IdTokenExpiresOn.Should().BeNone();
        _authTokens.Events.Last().Should().BeOfType<TokensRevoked>();
    }
}