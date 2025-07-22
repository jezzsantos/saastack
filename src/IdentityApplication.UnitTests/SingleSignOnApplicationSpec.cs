using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityApplication.UnitTests;

[Trait("Category", "Unit")]
public class SingleSignOnApplicationSpec
{
    private readonly Mock<IIdentityServerSingleSignOnService> _ssoService;
    private readonly SingleSignOnApplication _application;
    private readonly Mock<ICallerContext> _caller;
    public SingleSignOnApplicationSpec()
    {
        _caller = new Mock<ICallerContext>();
        _caller.Setup(cc => cc.CallerId)
            .Returns("acallerid");
        _ssoService = new Mock<IIdentityServerSingleSignOnService>();
        var identityServerProvider = new Mock<IIdentityServerProvider>();
        identityServerProvider.Setup(p => p.SingleSignOnService)
            .Returns(_ssoService.Object);

        _application = new SingleSignOnApplication(identityServerProvider.Object);
    }

    [Fact]
    public async Task WhenAuthenticate_ThenAuthenticates()
    {
        var expectedTokens = new AuthenticateTokens
        {
            AccessToken = new AuthenticationToken
            {
                Value = "anaccesstoken",
                ExpiresOn = DateTime.UtcNow.AddHours(1),
                Type = TokenType.AccessToken
            },
            RefreshToken = new AuthenticationToken
            {
                Value = "arefreshtoken",
                ExpiresOn = DateTime.UtcNow.AddDays(30),
                Type = TokenType.RefreshToken
            },
            UserId = "auserid"
        };

        _ssoService.Setup(sso => sso.AuthenticateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTokens);

        var result = await _application.AuthenticateAsync(_caller.Object, "aninvitationtoken", "aprovidername",
            "anauthcode", "ausername", true, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeEquivalentTo(expectedTokens);
        _ssoService.Verify(sso => sso.AuthenticateAsync(_caller.Object, "aninvitationtoken", "aprovidername",
            "anauthcode", "ausername", true, CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task WhenGetTokensAsync_ThenReturnsTokens()
    {
        var expectedTokens = new List<ProviderAuthenticationTokens>
        {
            new()
            {
                Provider = "aprovidername",
                AccessToken = new AuthenticationToken
                {
                    Value = "anaccesstoken",
                    ExpiresOn = DateTime.UtcNow.AddHours(1),
                    Type = TokenType.AccessToken
                },
                RefreshToken = new AuthenticationToken
                {
                    Value = "arefreshtoken",
                    ExpiresOn = DateTime.UtcNow.AddDays(30),
                    Type = TokenType.RefreshToken
                },
                OtherTokens = new List<AuthenticationToken>()
            }
        };

        _ssoService.Setup(sso => sso.GetTokensForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTokens);

        var result = await _application.GetTokensAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeEquivalentTo(expectedTokens);
        _ssoService.Verify(sso => sso.GetTokensForUserAsync(_caller.Object, "acallerid", CancellationToken.None),
            Times.Once);
    }
    [Fact]
    public async Task WhenGetTokensOnBehalfOfUserAsync_ThenReturnsTokens()
    {
        var expectedTokens = new List<ProviderAuthenticationTokens>
        {
            new()
            {
                Provider = "aprovidername",
                AccessToken = new AuthenticationToken
                {
                    Value = "anaccesstoken",
                    ExpiresOn = DateTime.UtcNow.AddHours(1),
                    Type = TokenType.AccessToken
                },
                RefreshToken = new AuthenticationToken
                {
                    Value = "arefreshtoken",
                    ExpiresOn = DateTime.UtcNow.AddDays(30),
                    Type = TokenType.RefreshToken
                },
                OtherTokens = new List<AuthenticationToken>()
            }
        };

        _ssoService.Setup(sso => sso.GetTokensForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTokens);

        var result = await _application.GetTokensOnBehalfOfUserAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeEquivalentTo(expectedTokens);
        _ssoService.Verify(sso => sso.GetTokensForUserAsync(_caller.Object, "auserid", CancellationToken.None),
            Times.Once);
    }

    [Fact]
    public async Task WhenRefreshTokenAsync_ThenRefreshes()
    {
        var expectedTokens = new ProviderAuthenticationTokens
        {
            Provider = "aprovidername",
            AccessToken = new AuthenticationToken
            {
                Value = "anewccesstoken",
                ExpiresOn = DateTime.UtcNow.AddHours(1),
                Type = TokenType.AccessToken
            },
            RefreshToken = new AuthenticationToken
            {
                Value = "anewrefreshtoken",
                ExpiresOn = DateTime.UtcNow.AddDays(30),
                Type = TokenType.RefreshToken
            },
            OtherTokens = new List<AuthenticationToken>()
        };

        _ssoService.Setup(sso => sso.RefreshTokenForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTokens);

        var result = await _application.RefreshTokenAsync(_caller.Object, "aprovidername", "arefreshtoken",
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeEquivalentTo(expectedTokens);
        _ssoService.Verify(sso => sso.RefreshTokenForUserAsync(_caller.Object, "acallerid", "aprovidername",
            "arefreshtoken", CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task WhenRefreshTokenOnBehalfOfUserAsync_ThenRefreshes()
    {
        var expectedTokens = new ProviderAuthenticationTokens
        {
            Provider = "aprovidername",
            AccessToken = new AuthenticationToken
            {
                Value = "anewccesstoken",
                ExpiresOn = DateTime.UtcNow.AddHours(1),
                Type = TokenType.AccessToken
            },
            RefreshToken = new AuthenticationToken
            {
                Value = "anewrefreshtoken",
                ExpiresOn = DateTime.UtcNow.AddDays(30),
                Type = TokenType.RefreshToken
            },
            OtherTokens = new List<AuthenticationToken>()
        };

        _ssoService.Setup(sso => sso.RefreshTokenForUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedTokens);

        var result = await _application.RefreshTokenOnBehalfOfUserAsync(_caller.Object, "auserid", "aprovidername",
            "arefreshtoken", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeEquivalentTo(expectedTokens);
        _ssoService.Verify(sso => sso.RefreshTokenForUserAsync(_caller.Object, "auserid", "aprovidername",
            "arefreshtoken", CancellationToken.None), Times.Once);
    }
}