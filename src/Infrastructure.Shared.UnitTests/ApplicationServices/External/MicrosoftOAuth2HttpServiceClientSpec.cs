using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices.External;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Shared.UnitTests.ApplicationServices.External;

[Trait("Category", "Unit")]
public class MicrosoftOAuth2HttpServiceClientSpec
{
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IOAuth2Service> _oauth2Service;
    private readonly MicrosoftOAuth2HttpServiceClient _serviceClient;

    public MicrosoftOAuth2HttpServiceClientSpec()
    {
        _caller = new Mock<ICallerContext>();
        _oauth2Service = new Mock<IOAuth2Service>();

        _serviceClient = new MicrosoftOAuth2HttpServiceClient(_oauth2Service.Object);
    }

    [Fact]
    public async Task WhenExchangeCodeForTokensAsync_ThenDelegates()
    {
        var tokens = new List<AuthToken>();
        _oauth2Service.Setup(oas => oas.ExchangeCodeForTokensAsync(It.IsAny<ICallerContext>(),
                It.IsAny<OAuth2CodeTokenExchangeOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);
        var options = new OAuth2CodeTokenExchangeOptions("aservicename", "acode");

        var result = await _serviceClient.ExchangeCodeForTokensAsync(_caller.Object,
            options, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeSameAs(tokens);
        _oauth2Service.Verify(oas => oas.ExchangeCodeForTokensAsync(_caller.Object,
            options, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRefreshTokenAsync_ThenDelegates()
    {
        var tokens = new List<AuthToken>();
        _oauth2Service.Setup(oas => oas.RefreshTokenAsync(It.IsAny<ICallerContext>(),
                It.IsAny<OAuth2RefreshTokenOptions>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokens);
        var options = new OAuth2RefreshTokenOptions("aservicename", "arefreshtoken");

        var result = await _serviceClient.RefreshTokenAsync(_caller.Object,
            options, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Should().BeSameAs(tokens);
        _oauth2Service.Verify(oas => oas.RefreshTokenAsync(_caller.Object,
            options, It.IsAny<CancellationToken>()));
    }
}