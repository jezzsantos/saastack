using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using FluentAssertions;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityApplication.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class NativeIdentityServerOpenIdConnectServiceSpec
{
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IOAuth2ClientService> _oauth2ClientService;
    private readonly Mock<IOidcAuthorizationRepository> _repository;
    private readonly NativeIdentityServerOpenIdConnectService _service;
    private readonly Mock<IWebsiteUiService> _websiteUiService;

    public NativeIdentityServerOpenIdConnectServiceSpec()
    {
        _caller = new Mock<ICallerContext>();
        var recorder = new Mock<IRecorder>();
        var identifierFactory = new Mock<IIdentifierFactory>();
        _oauth2ClientService = new Mock<IOAuth2ClientService>();
        _websiteUiService = new Mock<IWebsiteUiService>();
        _websiteUiService.Setup(wus => wus.ConstructLoginPageUrl())
            .Returns("aloginredirecturi");
        _websiteUiService.Setup(wus => wus.ConstructOAuth2ConsentPageUrl())
            .Returns("aconsentredirecturi");
        _repository = new Mock<IOidcAuthorizationRepository>();
        _service = new NativeIdentityServerOpenIdConnectService(recorder.Object, identifierFactory.Object,
            _websiteUiService.Object, _oauth2ClientService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenAuthorizeAsyncAndNotAuthenticated_ThenReturnsLoginRedirect()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);

        var result = await _service.AuthorizeAsync(_caller.Object, "aclientid", "auserid", "aredirecturi",
            OAuth2Constants.ResponseTypes.Code,
            $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile}", null, null, null, null,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.RedirectUri.Should().Be("aloginredirecturi");
        _websiteUiService.Verify(wus => wus.ConstructLoginPageUrl());
        _oauth2ClientService.Verify(ocs => ocs.FindClientByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenAuthorizeAsyncAndClientNotFound_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _oauth2ClientService.Setup(ocs => ocs.FindClientByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<OAuth2Client>.None);

        var result = await _service.AuthorizeAsync(_caller.Object, "aclientid", "auserid", "aredirecturi",
            OAuth2Constants.ResponseTypes.Code, "ascope", null, null, null, null, CancellationToken.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.NativeIdentityServerOpenIdConnectService_Authorize_UnknownClient);
        _websiteUiService.Verify(wus => wus.ConstructLoginPageUrl(), Times.Never);
        _oauth2ClientService.Verify(ocs =>
            ocs.FindClientByIdAsync(_caller.Object, "aclientid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAuthorizeAsyncAndClientNotConsentedByUserThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        var client = new OAuth2Client
        {
            Id = "aclientid",
            Name = "aclientname"
        };
        _oauth2ClientService.Setup(ocs => ocs.FindClientByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(client.ToOptional());
        _oauth2ClientService.Setup(ocs => ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.AuthorizeAsync(_caller.Object, "aclientid", "auserid",
            "aredirecturi", OAuth2Constants.ResponseTypes.Code, "ascope", null, null, null, null,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.RedirectUri.Should().Be("aconsentredirecturi");
        _websiteUiService.Verify(wus => wus.ConstructLoginPageUrl(), Times.Never);
        _websiteUiService.Verify(wus => wus.ConstructOAuth2ConsentPageUrl());
        _oauth2ClientService.Verify(ocs =>
            ocs.FindClientByIdAsync(_caller.Object, "aclientid", It.IsAny<CancellationToken>()));
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(_caller.Object, "aclientid", "auserid", "ascope",
                It.IsAny<CancellationToken>()));
    }
}