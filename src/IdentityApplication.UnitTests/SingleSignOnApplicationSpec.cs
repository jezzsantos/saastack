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
        _ssoProvidersService
            .Setup(sps => sps.FindByProviderNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ssoProvider.Object.ToOptional());
        _ssoProvidersService.Setup(sps =>
                sps.FindByUserIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(_ssoProvider.Object.ToOptional());
        _authTokensService = new Mock<IAuthTokensService>();

        _application = new SingleSignOnApplication(recorder.Object, _endUsersService.Object,
            _ssoProvidersService.Object,
            _authTokensService.Object);
    }

    [Fact]
    public async Task WhenAuthenticateAndNoProvider_ThenReturnsError()
    {
        _ssoProvidersService.Setup(sp => sp.FindByProviderNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<ISSOAuthenticationProvider>.None);

        var result = await _application.AuthenticateAsync(_caller.Object, "aninvitationtoken", "aprovidername",
            "anauthcode", null, CancellationToken.None);

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

        result.Should().BeError(ErrorCode.NotAuthenticated);
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
            sps => sps.SaveUserInfoAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<SSOUserInfo>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "anexistinguserid", It.IsAny<CancellationToken>()));
        _authTokensService.Verify(
            ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<EndUserWithMemberships>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenAuthenticateAndPersonIsSuspended_ThenIssuesToken()
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

        result.Should().BeError(ErrorCode.EntityLocked, Resources.SingleSignOnApplication_AccountSuspended);
        _endUsersService.Verify(eus =>
            eus.FindPersonByEmailPrivateAsync(_caller.Object, "auser@company.com", It.IsAny<CancellationToken>()));
        _endUsersService.Verify(
            eus => eus.RegisterPersonPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _ssoProvidersService.Verify(
            sps => sps.SaveUserInfoAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<SSOUserInfo>(),
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
                Access = EndUserAccess.Enabled,
                Memberships =
                [
                    new Membership
                    {
                        Id = "amembershipid",
                        IsDefault = true,
                        OrganizationId = "anorganizationid",
                        UserId = "auserid"
                    }
                ]
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
        _ssoProvidersService.Verify(sps => sps.SaveUserInfoAsync(_caller.Object, "aprovidername",
            "aregistereduserid".ToId(),
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
                Access = EndUserAccess.Enabled,
                Memberships =
                [
                    new Membership
                    {
                        Id = "amembershipid",
                        IsDefault = true,
                        OrganizationId = "anorganizationid",
                        UserId = "auserid"
                    }
                ]
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
        _ssoProvidersService.Verify(sps => sps.SaveUserInfoAsync(_caller.Object, "aprovidername",
            "anexistinguserid".ToId(),
            It.Is<SSOUserInfo>(ui => ui == userInfo), It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "anexistinguserid", It.IsAny<CancellationToken>()));
        _authTokensService.Verify(ats => ats.IssueTokensAsync(_caller.Object, It.Is<EndUserWithMemberships>(eu =>
            eu.Id == "amembershipsuserid"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRefreshTokenOnBehalfOfUserAsyncAndProviderUserNotExists_ThenReturnsError()
    {
        _ssoProvidersService.Setup(sp =>
                sp.FindByUserIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<ISSOAuthenticationProvider>.None);

        var result = await _application.RefreshTokenOnBehalfOfUserAsync(_caller.Object, "auserid", "aprovidername",
            "arefreshtoken", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        result.Error.AdditionalData.Should().OnlyContain(x =>
            x.Key == SingleSignOnApplication.AuthErrorProviderName && (string)x.Value == "aprovidername");
        _endUsersService.Verify(
            eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _ssoProvidersService.Verify(
            sps => sps.SaveUserTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ProviderAuthenticationTokens>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenOnBehalfOfUserAsyncAndEndUserNotExists_ThenReturnsError()
    {
        _endUsersService.Setup(eus =>
                eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.EntityNotFound());

        var result = await _application.RefreshTokenOnBehalfOfUserAsync(_caller.Object, "auserid", "aprovidername",
            "arefreshtoken", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        result.Error.AdditionalData.Should().OnlyContain(x =>
            x.Key == SingleSignOnApplication.AuthErrorProviderName && (string)x.Value == "aprovidername");
        _endUsersService.Verify(
            eus => eus.GetUserPrivateAsync(_caller.Object, It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        _ssoProvider.Verify(
            sop => sop.RefreshTokenAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _ssoProvidersService.Verify(
            sps => sps.SaveUserTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ProviderAuthenticationTokens>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenOnBehalfOfUserAsyncAndPersonIsSuspended_ThenReturnsError()
    {
        _endUsersService.Setup(eus =>
                eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUser
            {
                Id = "auserid",
                Classification = EndUserClassification.Person,
                Access = EndUserAccess.Suspended
            });
        _ssoProvider.Setup(sp =>
                sp.RefreshTokenAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Unexpected("amessage"));

        var result = await _application.RefreshTokenOnBehalfOfUserAsync(_caller.Object, "auserid", "aprovidername",
            "arefreshtoken", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityExists, Resources.SingleSignOnApplication_AccountSuspended);
        _endUsersService.Verify(
            eus => eus.GetUserPrivateAsync(_caller.Object, It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        _ssoProvider.Verify(
            sop => sop.RefreshTokenAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _ssoProvidersService.Verify(
            sps => sps.SaveUserTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ProviderAuthenticationTokens>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenOnBehalfOfUserAsyncAndRefreshErrors_ThenReturnsError()
    {
        _endUsersService.Setup(eus =>
                eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUser
            {
                Id = "auserid",
                Classification = EndUserClassification.Person,
                Access = EndUserAccess.Enabled
            });
        _ssoProvider.Setup(sp =>
                sp.RefreshTokenAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Unexpected("amessage"));

        var result = await _application.RefreshTokenOnBehalfOfUserAsync(_caller.Object, "auserid", "aprovidername",
            "arefreshtoken", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        result.Error.AdditionalData.Should().OnlyContain(x =>
            x.Key == SingleSignOnApplication.AuthErrorProviderName && (string)x.Value == "aprovidername");
        _endUsersService.Verify(
            eus => eus.GetUserPrivateAsync(_caller.Object, It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        _ssoProvider.Verify(
            sop => sop.RefreshTokenAsync(_caller.Object, It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        _ssoProvidersService.Verify(
            sps => sps.SaveUserTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<ProviderAuthenticationTokens>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenOnBehalfOfUserAsync_ThenRefreshed()
    {
        _endUsersService.Setup(eus =>
                eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EndUser
            {
                Id = "auserid",
                Classification = EndUserClassification.Person,
                Access = EndUserAccess.Enabled
            });
        _ssoProvider.Setup(sp =>
                sp.RefreshTokenAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProviderAuthenticationTokens
            {
                Provider = "aprovidername",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = default,
                    Type = TokenType.AccessToken,
                    Value = "anaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = default,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                },
                OtherTokens =
                [
                    new AuthenticationToken
                    {
                        ExpiresOn = default,
                        Type = TokenType.OtherToken,
                        Value = "anothertoken"
                    }
                ]
            });

        var result = await _application.RefreshTokenOnBehalfOfUserAsync(_caller.Object, "auserid", "aprovidername",
            "arefreshtoken", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Provider.Should().Be("aprovidername");
        result.Value.AccessToken.Type.Should().Be(TokenType.AccessToken);
        result.Value.AccessToken.Value.Should().Be("anaccesstoken");
        result.Value.RefreshToken!.Type.Should().Be(TokenType.RefreshToken);
        result.Value.RefreshToken.Value.Should().Be("arefreshtoken");
        result.Value.OtherTokens.Count.Should().Be(1);
        result.Value.OtherTokens[0].Type.Should().Be(TokenType.OtherToken);
        result.Value.OtherTokens[0].Value.Should().Be("anothertoken");
        _endUsersService.Verify(
            eus => eus.GetUserPrivateAsync(_caller.Object, It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        _ssoProvider.Verify(
            sop => sop.RefreshTokenAsync(_caller.Object, It.IsAny<string>(),
                It.IsAny<CancellationToken>()));
        _ssoProvidersService.Verify(
            sps => sps.SaveUserTokensAsync(_caller.Object, "aprovidername", "auserid".ToId(),
                It.Is<ProviderAuthenticationTokens>(token =>
                    token.Provider == "aprovidername"
                    && token.AccessToken.Value == "anaccesstoken"
                    && token.RefreshToken!.Value == "arefreshtoken"
                    && token.OtherTokens.Count == 1
                    && token.OtherTokens[0].Value == "anothertoken"
                ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenGetTokensAsync_ThenReturnsTokens()
    {
        var datum = DateTime.UtcNow;
        _ssoProvidersService.Setup(sps =>
                sps.GetTokensOnBehalfOfUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProviderAuthenticationTokens>
            {
                new()
                {
                    Provider = "aprovidername",
                    AccessToken = new AuthenticationToken
                    {
                        ExpiresOn = datum,
                        Type = TokenType.AccessToken,
                        Value = "anaccesstoken"
                    },
                    RefreshToken = new AuthenticationToken
                    {
                        ExpiresOn = datum,
                        Type = TokenType.RefreshToken,
                        Value = "arefreshtoken"
                    },
                    OtherTokens =
                    [
                        new AuthenticationToken
                        {
                            ExpiresOn = datum,
                            Type = TokenType.OtherToken,
                            Value = "anothertoken"
                        }
                    ]
                }
            });

        var result = await _application.GetTokensOnBehalfOfUserAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Count.Should().Be(1);
        result.Value[0].Provider.Should().Be("aprovidername");
        result.Value[0].AccessToken.ExpiresOn.Should().Be(datum);
        result.Value[0].AccessToken.Type.Should().Be(TokenType.AccessToken);
        result.Value[0].AccessToken.Value.Should().Be("anaccesstoken");
        result.Value[0].RefreshToken!.ExpiresOn.Should().Be(datum);
        result.Value[0].RefreshToken!.Type.Should().Be(TokenType.RefreshToken);
        result.Value[0].RefreshToken!.Value.Should().Be("arefreshtoken");
        result.Value[0].OtherTokens.Count.Should().Be(1);
        result.Value[0].OtherTokens[0].ExpiresOn.Should().Be(datum);
        result.Value[0].OtherTokens[0].Type.Should().Be(TokenType.OtherToken);
        result.Value[0].OtherTokens[0].Value.Should().Be("anothertoken");
    }
}