using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using FluentAssertions;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain;
using Moq;
using UnitTesting.Common;
using Xunit;
using OAuth2ResponseType = Application.Resources.Shared.OAuth2ResponseType;
using OAuth2TokenType = Application.Resources.Shared.OAuth2TokenType;
using OAuth2CodeChallengeMethod = Domain.Shared.Identities.OAuth2CodeChallengeMethod;

namespace IdentityApplication.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class NativeIdentityServerOpenIdConnectServiceSpec
{
    private readonly Mock<IAuthTokensService> _authTokensService;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEndUsersService> _endUsersService;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IOAuth2ClientService> _oauth2ClientService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IOidcAuthorizationRepository> _repository;
    private readonly NativeIdentityServerOpenIdConnectService _service;
    private readonly Mock<ITokensService> _tokensService;
    private readonly Mock<IUserProfilesService> _userProfilesService;
    private readonly Mock<IWebsiteUiService> _websiteUiService;

    public NativeIdentityServerOpenIdConnectServiceSpec()
    {
        _caller = new Mock<ICallerContext>();
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _oauth2ClientService = new Mock<IOAuth2ClientService>();
        _websiteUiService = new Mock<IWebsiteUiService>();
        _websiteUiService.Setup(wus => wus.ConstructLoginPageUrl())
            .Returns("aloginredirecturi");
        _websiteUiService.Setup(wus => wus.ConstructOAuth2ConsentPageUrl())
            .Returns("aconsentredirecturi");
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateOAuthAuthorizationCode(It.IsAny<string>()))
            .Returns("anauthorizationcode");
        _authTokensService = new Mock<IAuthTokensService>();
        _endUsersService = new Mock<IEndUsersService>();
        _userProfilesService = new Mock<IUserProfilesService>();
        _repository = new Mock<IOidcAuthorizationRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<OidcAuthorizationRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OidcAuthorizationRoot root, CancellationToken _) => root);

        _service = new NativeIdentityServerOpenIdConnectService(_recorder.Object, _idFactory.Object,
            _tokensService.Object, _websiteUiService.Object, _oauth2ClientService.Object, _authTokensService.Object,
            _endUsersService.Object, _userProfilesService.Object, _repository.Object);
    }

    [Fact]
    public async Task WhenAuthorizeAsyncAndNotAuthenticated_ThenRedirectToLoginPage()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);

        var result = await _service.AuthorizeAsync(_caller.Object, "aclientid", "auserid", "aredirecturi",
            OAuth2ResponseType.Code, $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile}", null,
            null, null, null, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.RawRedirectUri.Should().Be("aloginredirecturi");
        _websiteUiService.Verify(wus => wus.ConstructLoginPageUrl());
        _oauth2ClientService.Verify(ocs => ocs.FindClientByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenAuthorizeAsyncAndWrongResponseType_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);

        var result = await _service.AuthorizeAsync(_caller.Object, "aclientid", "auserid", "aredirecturi",
            OAuth2ResponseType.Token, $"{OpenIdConnectConstants.Scopes.OpenId}", null, null, null, null,
            CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation,
            Resources.NativeIdentityServerOpenIdConnectService_Authorize_UnsupportedResponseType.Format(
                OAuth2ResponseType.Token));
        _websiteUiService.Verify(wus => wus.ConstructLoginPageUrl(), Times.Never);
        _oauth2ClientService.Verify(ocs =>
                ocs.FindClientByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenAuthorizeAsyncAndScopeMissingOpenId_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);

        var result = await _service.AuthorizeAsync(_caller.Object, "aclientid", "auserid", "aredirecturi",
            OAuth2ResponseType.Code, $"{OAuth2Constants.Scopes.Profile}", null, null, null, null,
            CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation,
            Resources.NativeIdentityServerOpenIdConnectService_Authorize_MissingOpenIdScope);
        _websiteUiService.Verify(wus => wus.ConstructLoginPageUrl(), Times.Never);
        _oauth2ClientService.Verify(ocs =>
                ocs.FindClientByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenAuthorizeAsyncAndPkceCodeChallengeMissingMethod_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);

        var result = await _service.AuthorizeAsync(_caller.Object, "aclientid", "auserid", "aredirecturi",
            OAuth2ResponseType.Code, $"{OpenIdConnectConstants.Scopes.OpenId}", null, null, "acodechallenge", null,
            CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation,
            Resources.NativeIdentityServerOpenIdConnectService_Authorize_MissingCodeChallengeMethod);
        _websiteUiService.Verify(wus => wus.ConstructLoginPageUrl(), Times.Never);
        _oauth2ClientService.Verify(ocs =>
                ocs.FindClientByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
            OAuth2ResponseType.Code, $"{OpenIdConnectConstants.Scopes.OpenId}", null, null, null, null,
            CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation,
            Resources.NativeIdentityServerOpenIdConnectService_Authorize_UnknownClient);
        _websiteUiService.Verify(wus => wus.ConstructLoginPageUrl(), Times.Never);
        _oauth2ClientService.Verify(ocs =>
            ocs.FindClientByIdAsync(_caller.Object, "aclientid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAuthorizeAsyncAndClientRequestUriNotMatch_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        var client = new OAuth2Client
        {
            Id = "aclientid",
            Name = "aclientname",
            RedirectUri = "aredirecturi"
        };
        _oauth2ClientService.Setup(ocs => ocs.FindClientByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(client.ToOptional());
        _oauth2ClientService.Setup(ocs => ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.AuthorizeAsync(_caller.Object, "aclientid", "auserid",
            "anotherredirecturi", OAuth2ResponseType.Code, $"{OpenIdConnectConstants.Scopes.OpenId}", null, null, null,
            null, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation,
            Resources.NativeIdentityServerOpenIdConnectService_Authorize_MismatchedRequestUri.Format(
                "anotherredirecturi"));
        _websiteUiService.Verify(wus => wus.ConstructLoginPageUrl(), Times.Never);
        _websiteUiService.Verify(wus => wus.ConstructOAuth2ConsentPageUrl(), Times.Never);
        _oauth2ClientService.Verify(ocs =>
            ocs.FindClientByIdAsync(_caller.Object, "aclientid", It.IsAny<CancellationToken>()));
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenAuthorizeAsyncAndClientNotConsentedByUser_ThenRedirectsToConsentPage()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        var client = new OAuth2Client
        {
            Id = "aclientid",
            Name = "aclientname",
            RedirectUri = "aredirecturi"
        };
        _oauth2ClientService.Setup(ocs => ocs.FindClientByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(client.ToOptional());
        _oauth2ClientService.Setup(ocs => ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.AuthorizeAsync(_caller.Object, "aclientid", "auserid",
            "aredirecturi", OAuth2ResponseType.Code, $"{OpenIdConnectConstants.Scopes.OpenId}", null, null,
            null, null, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.RawRedirectUri.Should().Be("aconsentredirecturi");
        _websiteUiService.Verify(wus => wus.ConstructLoginPageUrl(), Times.Never);
        _websiteUiService.Verify(wus => wus.ConstructOAuth2ConsentPageUrl());
        _oauth2ClientService.Verify(ocs =>
            ocs.FindClientByIdAsync(_caller.Object, "aclientid", It.IsAny<CancellationToken>()));
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(_caller.Object, "aclientid", "auserid",
                $"{OpenIdConnectConstants.Scopes.OpenId}",
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenAuthorizeAsync_ThenRedirectsWithAuthorizationCode()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        var client = new OAuth2Client
        {
            Id = "aclientid",
            Name = "aclientname",
            RedirectUri = "aredirecturi"
        };
        _oauth2ClientService.Setup(ocs => ocs.FindClientByIdAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(client.ToOptional());
        _oauth2ClientService.Setup(ocs => ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repository.Setup(rep =>
                rep.FindByClientAndUserAsync(It.IsAny<Identifier>(), It.IsAny<Identifier>(),
                    It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<OidcAuthorizationRoot>.None);

        var result = await _service.AuthorizeAsync(_caller.Object, "aclientid", "auserid",
            "aredirecturi", OAuth2ResponseType.Code, $"{OpenIdConnectConstants.Scopes.OpenId}", "astate", "anonce",
            "acodechallenge", Application.Resources.Shared.OAuth2CodeChallengeMethod.Plain, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.RawRedirectUri.Should().Be("aconsentredirecturi");

        result.Value.RawRedirectUri.Should().BeNull();
        result.Value.Code!.Code.Should().Be("aclientid:anauthorizationcode");
        result.Value.Code.State.Should().Be("astate");
        _websiteUiService.Verify(wus => wus.ConstructLoginPageUrl(), Times.Never);
        _websiteUiService.Verify(wus => wus.ConstructOAuth2ConsentPageUrl(), Times.Never);
        _oauth2ClientService.Verify(ocs =>
            ocs.FindClientByIdAsync(_caller.Object, "aclientid", It.IsAny<CancellationToken>()));
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(_caller.Object, "aclientid", "auserid",
                $"{OpenIdConnectConstants.Scopes.OpenId}",
                It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.SaveAsync(It.Is<OidcAuthorizationRoot>(root =>
            root.AuthorizationCode.Value == "aclientid:anauthorizationcode"
            && root.AuthorizationExpiresAt.Value.IsNear(
                DateTime.UtcNow.Add(OidcAuthorizationRoot.DefaultAuthorizationCodeExpiry))
            && root.CodeChallenge.Value == "acodechallenge"
            && root.CodeChallengeMethod.Value == OAuth2CodeChallengeMethod.Plain
            && root.Nonce.Value == "anonce"
            && root.RedirectUri.Value == "aredirecturi"
            && root.Scopes.Value.Items.SequenceEqual(new List<string> { OpenIdConnectConstants.Scopes.OpenId })
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenExchangeCodeForTokensAsyncAndClientVerificationFails_ThenReturnsError()
    {
        _oauth2ClientService.Setup(ocs => ocs.VerifyClientAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Validation("anerror"));

        var result = await _service.ExchangeCodeForTokensAsync(_caller.Object, "aclientid", "aclientsecret",
            "anauthorizationcode", "aredirecturi", null, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation,
            Resources.NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_UnknownClient);
        _repository.Verify(rep =>
                rep.FindByAuthorizationCodeAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenExchangeCodeForTokensAsyncAndAuthorizationNotFound_ThenReturnsError()
    {
        var client = new OAuth2Client
        {
            Id = "aclientid",
            Name = "aclientname",
            RedirectUri = "aredirecturi"
        };
        _oauth2ClientService.Setup(ocs => ocs.VerifyClientAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        _repository.Setup(rep => rep.FindByAuthorizationCodeAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<OidcAuthorizationRoot>.None);

        var result = await _service.ExchangeCodeForTokensAsync(_caller.Object, "aclientid", "aclientsecret",
            "anauthorizationcode", "aredirecturi", null, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation,
            Resources.NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_UnknownAuthorizationCode);
        _oauth2ClientService.Verify(ocs =>
            ocs.VerifyClientAsync(_caller.Object, "aclientid", "aclientsecret", It.IsAny<CancellationToken>()));
        _repository.Verify(rep =>
            rep.FindByAuthorizationCodeAsync("aclientid".ToId(), "anauthorizationcode",
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenExchangeCodeForTokensAsyncAndFetchingEndUserFails_ThenReturnsError()
    {
        var client = new OAuth2Client
        {
            Id = "aclientid",
            Name = "aclientname",
            RedirectUri = "aredirecturi"
        };
        _oauth2ClientService.Setup(ocs => ocs.VerifyClientAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        var authorization = CreateAuthorizedAuthorization();
        _repository.Setup(rep => rep.FindByAuthorizationCodeAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        _endUsersService.Setup(eus => eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Unexpected("anerror"));

        var result = await _service.ExchangeCodeForTokensAsync(_caller.Object, "aclientid", "aclientsecret",
            "anauthorizationcode", "aredirecturi", null, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation,
            Resources.NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_MissingUser);
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
                ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenExchangeCodeForTokensAsyncAndFetchingUserProfilesFails_ThenReturnsError()
    {
        var client = new OAuth2Client
        {
            Id = "aclientid",
            Name = "aclientname",
            RedirectUri = "aredirecturi"
        };
        _oauth2ClientService.Setup(ocs => ocs.VerifyClientAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        var authorization = CreateAuthorizedAuthorization();
        _repository.Setup(rep => rep.FindByAuthorizationCodeAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        var user = new EndUserWithMemberships
        {
            Id = "auserid"
        };
        _endUsersService.Setup(eus => eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userProfilesService.Setup(ups => ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Unexpected("anerror"));

        var result = await _service.ExchangeCodeForTokensAsync(_caller.Object, "aclientid", "aclientsecret",
            "anauthorizationcode", "aredirecturi", null, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation,
            Resources.NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_MissingUserProfile);
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.Is<ICallerContext>(cc =>
                cc.CallerId == CallerConstants.MaintenanceAccountUserId
            ), "auserid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenExchangeCodeForTokensAsync_ThenReturnsToken()
    {
        var client = new OAuth2Client
        {
            Id = "aclientid",
            Name = "aclientname",
            RedirectUri = "aredirecturi"
        };
        _oauth2ClientService.Setup(ocs => ocs.VerifyClientAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);
        var authorization = CreateAuthorizedAuthorization(codeChallenge: "acodeverifier",
            codeChallengeMethod: OAuth2CodeChallengeMethod.Plain);
        _repository.Setup(rep => rep.FindByAuthorizationCodeAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        var user = new EndUserWithMemberships
        {
            Id = "auserid"
        };
        _endUsersService.Setup(eus => eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var profile = new UserProfile
        {
            Id = "auserid",
            UserId = "auserid",
            DisplayName = "adisplayname",
            Name = new PersonName
            {
                FirstName = "afirstname"
            },
            Classification = UserProfileClassification.Person
        };
        _userProfilesService.Setup(ups => ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        var issuedTokens = new AccessTokens("anaccesstoken", DateTime.UtcNow.AddHours(1), "arefreshtoken",
            DateTime.UtcNow.AddDays(30), "anidtoken", DateTime.UtcNow.AddHours(1));
        _authTokensService.Setup(ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(),
                It.IsAny<EndUserWithMemberships>(), It.IsAny<UserProfile>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(issuedTokens);

        var result = await _service.ExchangeCodeForTokensAsync(_caller.Object, "aclientid", "aclientsecret",
            "anauthorizationcode", "aredirecturi", "acodeverifier", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AccessToken.Should().Be("anaccesstoken");
        result.Value.TokenType.Should().Be(OAuth2TokenType.Bearer);
        result.Value.RefreshToken.Should().Be("arefreshtoken");
        result.Value.IdToken.Should().Be("anidtoken");
        _oauth2ClientService.Verify(ocs =>
            ocs.VerifyClientAsync(_caller.Object, "aclientid", "aclientsecret", It.IsAny<CancellationToken>()));
        _repository.Verify(rep =>
            rep.FindByAuthorizationCodeAsync("aclientid".ToId(), "anauthorizationcode",
                It.IsAny<CancellationToken>()));
        _repository.Verify(rep =>
            rep.SaveAsync(authorization, It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.Is<ICallerContext>(cc =>
                cc.CallerId == CallerConstants.MaintenanceAccountUserId
            ), "auserid", It.IsAny<CancellationToken>()));
        _authTokensService.Verify(ats =>
            ats.IssueTokensAsync(_caller.Object, user, profile, It.Is<IReadOnlyList<string>>(scopes =>
                    scopes.SequenceEqual(new List<string> { OpenIdConnectConstants.Scopes.OpenId })),
                It.Is<Dictionary<string, object>>(d => d.ContainsKey(AuthenticationConstants.Claims.ForNonce) &&
                                                       d[AuthenticationConstants.Claims.ForNonce].Equals("anonce")),
                It.IsAny<CancellationToken>()));
        _recorder.Verify(rec => rec.AuditAgainst(It.IsAny<ICallContext>(), "auserid",
            Audits.NativeIdentityServerOpenIdConnectService_Authorization_Passed,
            It.IsAny<string>(), It.IsAny<object[]>()));
    }

    private OidcAuthorizationRoot CreateAuthorization(string clientId = "aclientid", string userId = "auserid")
    {
        return OidcAuthorizationRoot.Create(_recorder.Object, _idFactory.Object, _tokensService.Object,
            clientId.ToId(), userId.ToId()).Value;
    }

    private OidcAuthorizationRoot CreateAuthorizedAuthorization(string clientId = "aclientid",
        string userId = "auserid",
        string redirectUri = "aredirecturi", string? nonce = "anonce", string? codeChallenge = null,
        OAuth2CodeChallengeMethod? codeChallengeMethod = null)
    {
        var authorization = CreateAuthorization(clientId, userId);
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        authorization.AuthorizeCode(redirectUri, redirectUri, scopes, nonce.ToOptional(),
            codeChallenge.ToOptional(), codeChallengeMethod.ToOptional());
        return authorization;
    }
}