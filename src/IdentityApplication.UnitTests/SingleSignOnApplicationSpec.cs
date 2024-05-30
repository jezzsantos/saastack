using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.ValueObjects;
using FluentAssertions;
using IdentityApplication.ApplicationServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityApplication.UnitTests;

[Trait("Category", "Unit")]
public class SingleSignOnApplicationSpec
{
    private readonly SingleSignOnApplication _application;
    private readonly Mock<IAuthTokensService> _authTokensService;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEndUsersService> _endUsersService;
    private readonly Mock<ISSOAuthenticationProvider> _ssoProvider;
    private readonly Mock<ISSOProvidersService> _ssoProvidersService;

    public SingleSignOnApplicationSpec()
    {
        var recorder = new Mock<IRecorder>();
        _caller = new Mock<ICallerContext>();
        _endUsersService = new Mock<IEndUsersService>();
        _ssoProvider = new Mock<ISSOAuthenticationProvider>();
        _ssoProvidersService = new Mock<ISSOProvidersService>();
        _ssoProvidersService.Setup(sps => sps.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ssoProvider.Object.ToOptional());
        _authTokensService = new Mock<IAuthTokensService>();

        _application = new SingleSignOnApplication(recorder.Object, _endUsersService.Object,
            _ssoProvidersService.Object,
            _authTokensService.Object);
    }

    [Fact]
    public async Task WhenAuthenticateAndNoProvider_ThenReturnsError()
    {
        _ssoProvidersService.Setup(sp => sp.FindByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<ISSOAuthenticationProvider>.None);

        var result = await _application.AuthenticateAsync(_caller.Object, "aninvitationtoken", "aprovidername",
            "anauthcode", null,
            CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _endUsersService.Verify(
            eus => eus.FindPersonByEmailPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenAuthenticateAndProviderErrors_ThenReturnsError()
    {
        _ssoProvider.Setup(sp =>
                sp.AuthenticateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Unexpected("amessage"));

        var result = await _application.AuthenticateAsync(_caller.Object, "aninvitationtoken", "aprovidername",
            "anauthcode", null,
            CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected, "amessage");
        _endUsersService.Verify(
            eus => eus.FindPersonByEmailPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenAuthenticateAndPersonExistsButNotRegisteredYet_ThenIssuesToken()
    {
        var userInfo = new SSOUserInfo(new List<AuthToken>(), "auser@company.com", "afirstname", null,
            Timezones.Default,
            CountryCodes.Default);
        _ssoProvider.Setup(sp =>
                sp.AuthenticateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);
        var endUser = new EndUser
        {
            Id = "anexistinguserid"
        };
        _endUsersService.Setup(eus =>
                eus.FindPersonByEmailPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(endUser.ToOptional());
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "amembershipsuserid",
                Status = EndUserStatus.Unregistered,
                Access = EndUserAccess.Enabled
            });
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(),
                It.IsAny<EndUserWithMemberships>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccessTokens("anaccesstoken", expiresOn, "arefreshtoken", expiresOn));

        var result = await _application.AuthenticateAsync(_caller.Object, "aninvitationtoken", "aprovidername",
            "anauthcode", null,
            CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        _endUsersService.Verify(eus =>
            eus.FindPersonByEmailPrivateAsync(_caller.Object, "auser@company.com", It.IsAny<CancellationToken>()));
        _endUsersService.Verify(
            eus => eus.RegisterPersonPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _ssoProvidersService.Verify(
            sps => sps.SaveUserInfoAsync(It.IsAny<string>(), It.IsAny<Identifier>(), It.IsAny<SSOUserInfo>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "anexistinguserid", It.IsAny<CancellationToken>()));
        _authTokensService.Verify(
            ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<EndUserWithMemberships>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenAuthenticateAndPersonExistsButSuspended_ThenIssuesToken()
    {
        var userInfo = new SSOUserInfo(new List<AuthToken>(), "auser@company.com", "afirstname", null,
            Timezones.Default,
            CountryCodes.Default);
        _ssoProvider.Setup(sp =>
                sp.AuthenticateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);
        var endUser = new EndUser
        {
            Id = "anexistinguserid"
        };
        _endUsersService.Setup(eus =>
                eus.FindPersonByEmailPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(endUser.ToOptional());
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "amembershipsuserid",
                Status = EndUserStatus.Registered,
                Access = EndUserAccess.Suspended
            });
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(),
                It.IsAny<EndUserWithMemberships>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccessTokens("anaccesstoken", expiresOn, "arefreshtoken", expiresOn));

        var result = await _application.AuthenticateAsync(_caller.Object, "aninvitationtoken", "aprovidername",
            "anauthcode", null,
            CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityExists, Resources.SingleSignOnApplication_AccountSuspended);
        _endUsersService.Verify(eus =>
            eus.FindPersonByEmailPrivateAsync(_caller.Object, "auser@company.com", It.IsAny<CancellationToken>()));
        _endUsersService.Verify(
            eus => eus.RegisterPersonPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _ssoProvidersService.Verify(
            sps => sps.SaveUserInfoAsync(It.IsAny<string>(), It.IsAny<Identifier>(), It.IsAny<SSOUserInfo>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "anexistinguserid", It.IsAny<CancellationToken>()));
        _authTokensService.Verify(
            ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<EndUserWithMemberships>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenAuthenticateAndPersonNotExists_ThenRegistersPersonAndIssuesToken()
    {
        var userInfo = new SSOUserInfo(new List<AuthToken>(), "auser@company.com", "afirstname", null, Timezones.Sydney,
            CountryCodes.Australia);
        _ssoProvider.Setup(sp =>
                sp.AuthenticateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);
        _endUsersService.Setup(eus =>
                eus.FindPersonByEmailPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional.None<EndUser>());
        _endUsersService.Setup(eus => eus.RegisterPersonPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUser
            {
                Id = "aregistereduserid"
            });
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "amembershipsuserid",
                Status = EndUserStatus.Registered,
                Access = EndUserAccess.Enabled
            });
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(),
                It.IsAny<EndUserWithMemberships>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccessTokens("anaccesstoken", expiresOn, "arefreshtoken", expiresOn));

        var result = await _application.AuthenticateAsync(_caller.Object, "aninvitationtoken", "aprovidername",
            "anauthcode", null, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AccessToken.Value.Should().Be("anaccesstoken");
        result.Value.AccessToken.ExpiresOn.Should().Be(expiresOn);
        result.Value.RefreshToken.Value.Should().Be("arefreshtoken");
        result.Value.RefreshToken.ExpiresOn.Should().Be(expiresOn);
        _endUsersService.Verify(eus =>
            eus.FindPersonByEmailPrivateAsync(_caller.Object, "auser@company.com", It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus => eus.RegisterPersonPrivateAsync(_caller.Object, "aninvitationtoken",
            "auser@company.com", "afirstname", null, Timezones.Sydney.ToString(), CountryCodes.Australia.ToString(),
            true, It.IsAny<CancellationToken>()));
        _ssoProvidersService.Verify(sps => sps.SaveUserInfoAsync("aprovidername", "aregistereduserid".ToId(),
            It.Is<SSOUserInfo>(ui => ui == userInfo), It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "aregistereduserid", It.IsAny<CancellationToken>()));
        _authTokensService.Verify(ats => ats.IssueTokensAsync(_caller.Object, It.Is<EndUserWithMemberships>(eu =>
            eu.Id == "amembershipsuserid"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAuthenticateAndPersonExists_ThenIssuesToken()
    {
        var userInfo = new SSOUserInfo(new List<AuthToken>(), "auser@company.com", "afirstname", null,
            Timezones.Default,
            CountryCodes.Default);
        _ssoProvider.Setup(sp =>
                sp.AuthenticateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(userInfo);
        var endUser = new EndUser
        {
            Id = "anexistinguserid"
        };
        _endUsersService.Setup(eus =>
                eus.FindPersonByEmailPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(endUser.ToOptional());
        _endUsersService.Setup(eus =>
                eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUserWithMemberships
            {
                Id = "amembershipsuserid",
                Status = EndUserStatus.Registered,
                Access = EndUserAccess.Enabled
            });
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(),
                It.IsAny<EndUserWithMemberships>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccessTokens("anaccesstoken", expiresOn, "arefreshtoken", expiresOn));

        var result = await _application.AuthenticateAsync(_caller.Object, "aninvitationtoken", "aprovidername",
            "anauthcode", null,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AccessToken.Value.Should().Be("anaccesstoken");
        result.Value.AccessToken.ExpiresOn.Should().Be(expiresOn);
        result.Value.RefreshToken.Value.Should().Be("arefreshtoken");
        result.Value.RefreshToken.ExpiresOn.Should().Be(expiresOn);
        _endUsersService.Verify(eus =>
            eus.FindPersonByEmailPrivateAsync(_caller.Object, "auser@company.com", It.IsAny<CancellationToken>()));
        _endUsersService.Verify(
            eus => eus.RegisterPersonPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _ssoProvidersService.Verify(sps => sps.SaveUserInfoAsync("aprovidername", "anexistinguserid".ToId(),
            It.Is<SSOUserInfo>(ui => ui == userInfo), It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "anexistinguserid", It.IsAny<CancellationToken>()));
        _authTokensService.Verify(ats => ats.IssueTokensAsync(_caller.Object, It.Is<EndUserWithMemberships>(eu =>
            eu.Id == "amembershipsuserid"
        ), It.IsAny<CancellationToken>()));
    }
}