using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
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
using Microsoft.IdentityModel.Tokens;
using Moq;
using UnitTesting.Common;
using Xunit;
using AuthToken = IdentityDomain.AuthToken;
using OAuth2ResponseType = Application.Resources.Shared.OAuth2ResponseType;
using OAuth2TokenType = Application.Resources.Shared.OAuth2TokenType;
using OpenIdConnectCodeChallengeMethod = Domain.Shared.Identities.OpenIdConnectCodeChallengeMethod;

namespace IdentityApplication.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class NativeIdentityServerOpenIdConnectServiceSpec
{
    private readonly Mock<IAuthTokensService> _authTokensService;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IEncryptionService> _encryptionService;
    private readonly Mock<IEndUsersService> _endUsersService;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IOAuth2ClientService> _oauth2ClientService;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<IOpenIdConnectAuthorizationRepository> _repository;
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
        var settings = new Mock<IConfigurationSettings>();
        settings.Setup(s =>
                s.Platform.GetString(NativeIdentityServerOpenIdConnectService.BaseUrlSettingName, It.IsAny<string>()))
            .Returns("abaseurl");
        settings.Setup(s =>
                s.Platform.GetString(NativeIdentityServerOpenIdConnectService.JWTSigningPublicKeySettingName, null))
            .Returns(
                "-----BEGIN RSA PUBLIC KEY-----\nMIIBCgKCAQEAoZlYKp93rSsf5ZO1Xzj857bqzASga+L9vfw3qAjEl3NqOOTILvtS\n+4Sw+IUZ0qv4xRTNXZmaLPy4fwLByCZX496y0buJ8ouvevR7etYFNn9NIKJcphV6\njRyHFG6YgUejHzcwdSyhKuc9kPEjuOGhjSs1+94+VJmrYqUjFDgjMOl/GqVQhHww\nQzbWiZD6gJiICXpIUpBo0K65TmwGBgm/Zj5ImZZI0aKmbLY4aod5LiTO8JCqzE9K\nphH/XBb7oXYlURQ8DHLFnfsIwO7VTD6qS6LiBY+9Vv6YFMJ4oinULOJJt9EJgvbC\nzbvSGiY37k6y4Dn2jbmwcDjE2J7z0muNmQIDAQAB\n-----END RSA PUBLIC KEY-----");
        _oauth2ClientService = new Mock<IOAuth2ClientService>();
        _websiteUiService = new Mock<IWebsiteUiService>();
        _websiteUiService.Setup(wus => wus.ConstructLoginPageUrl())
            .Returns("aloginredirecturi");
        _websiteUiService.Setup(wus => wus.ConstructOAuth2ConsentPageUrl(It.IsAny<string>(), It.IsAny<string>()))
            .Returns("aconsentredirecturi");
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateOAuthorizationCodeDigest(It.IsAny<string>()))
            .Returns("anauthorizationcode");
        _tokensService.Setup(ts => ts.CreateTokenDigest(It.IsAny<string>()))
            .Returns("adigestvalue");
        _encryptionService = new Mock<IEncryptionService>();
        _encryptionService.Setup(es => es.Decrypt(It.IsAny<string>()))
            .Returns("eyJadecryptedvalue");
        _encryptionService.Setup(es => es.Encrypt(It.IsAny<string>()))
            .Returns("anencryptedvalue");
        _authTokensService = new Mock<IAuthTokensService>();
        _endUsersService = new Mock<IEndUsersService>();
        _userProfilesService = new Mock<IUserProfilesService>();
        _repository = new Mock<IOpenIdConnectAuthorizationRepository>();
        _repository.Setup(rep =>
                rep.SaveAsync(It.IsAny<OpenIdConnectAuthorizationRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenIdConnectAuthorizationRoot root, CancellationToken _) => root);

        _service = new NativeIdentityServerOpenIdConnectService(_recorder.Object, _idFactory.Object, settings.Object,
            _encryptionService.Object, _tokensService.Object, _websiteUiService.Object, _oauth2ClientService.Object,
            _authTokensService.Object, _endUsersService.Object, _userProfilesService.Object, _repository.Object);
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
        _websiteUiService.Verify(wus => wus.ConstructOAuth2ConsentPageUrl(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
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
        _websiteUiService.Verify(wus => wus.ConstructOAuth2ConsentPageUrl(It.IsAny<string>(), It.IsAny<string>()));
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
            .ReturnsAsync(Optional<OpenIdConnectAuthorizationRoot>.None);

        var result = await _service.AuthorizeAsync(_caller.Object, "aclientid", "auserid",
            "aredirecturi", OAuth2ResponseType.Code, $"{OpenIdConnectConstants.Scopes.OpenId}", "astate", "anonce",
            "acodechallenge", Application.Resources.Shared.OpenIdConnectCodeChallengeMethod.Plain,
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.RawRedirectUri.Should().BeNull();
        result.Value.Code!.Code.Should().Be("aclientid:anauthorizationcode");
        result.Value.Code.State.Should().Be("astate");
        _websiteUiService.Verify(wus => wus.ConstructLoginPageUrl(), Times.Never);
        _websiteUiService.Verify(wus => wus.ConstructOAuth2ConsentPageUrl(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _oauth2ClientService.Verify(ocs =>
            ocs.FindClientByIdAsync(_caller.Object, "aclientid", It.IsAny<CancellationToken>()));
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(_caller.Object, "aclientid", "auserid",
                $"{OpenIdConnectConstants.Scopes.OpenId}",
                It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.SaveAsync(It.Is<OpenIdConnectAuthorizationRoot>(root =>
            root.AuthorizationCode.Value == "aclientid:anauthorizationcode"
            && root.AuthorizationExpiresAt.Value.IsNear(
                DateTime.UtcNow.Add(OpenIdConnectAuthorizationRoot.DefaultAuthorizationCodeExpiry))
            && root.CodeChallenge.Value == "acodechallenge"
            && root.CodeChallengeMethod.Value == OpenIdConnectCodeChallengeMethod.Plain
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
            .ReturnsAsync(Optional<OpenIdConnectAuthorizationRoot>.None);

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
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "eyJanaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                },
                IdToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.OtherToken,
                    Value = "eyJanidtoken"
                }
            });

        var result = await _service.ExchangeCodeForTokensAsync(_caller.Object, "aclientid", "aclientsecret",
            "anauthorizationcode", "aredirecturi", null, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation,
            Resources.NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_MissingUser);
        _authTokensService.Verify(ats =>
            ats.IssueTokensAsync(_caller.Object, "auserid",
                It.Is<IReadOnlyList<string>>(scopes =>
                    scopes.SequenceEqual(new List<string> { OpenIdConnectConstants.Scopes.OpenId })),
                It.Is<Dictionary<string, object>>(dic => dic.ContainsKey(AuthenticationConstants.Claims.ForNonce) &&
                                                         dic[AuthenticationConstants.Claims.ForNonce].Equals("anonce")
                                                         && dic.ContainsKey(AuthenticationConstants.Claims.ForClientId)
                                                         &&
                                                         dic[AuthenticationConstants.Claims.ForClientId]
                                                             .Equals("aclientid")),
                It.IsAny<CancellationToken>()));
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
            Id = "auserid",
            Classification = EndUserClassification.Person,
            Status = EndUserStatus.Registered
        };
        _endUsersService.Setup(eus => eus.GetMembershipsPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userProfilesService.Setup(ups => ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Unexpected("anerror"));
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "eyJanaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                },
                IdToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.OtherToken,
                    Value = "eyJanidtoken"
                }
            });

        var result = await _service.ExchangeCodeForTokensAsync(_caller.Object, "aclientid", "aclientsecret",
            "anauthorizationcode", "aredirecturi", null, CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation,
            Resources.NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_MissingUserProfile);
        _authTokensService.Verify(ats =>
            ats.IssueTokensAsync(_caller.Object, "auserid",
                It.Is<IReadOnlyList<string>>(scopes =>
                    scopes.SequenceEqual(new List<string> { OpenIdConnectConstants.Scopes.OpenId })),
                It.Is<Dictionary<string, object>>(dic => dic.ContainsKey(AuthenticationConstants.Claims.ForNonce) &&
                                                         dic[AuthenticationConstants.Claims.ForNonce].Equals("anonce")
                                                         && dic.ContainsKey(AuthenticationConstants.Claims.ForClientId)
                                                         &&
                                                         dic[AuthenticationConstants.Claims.ForClientId]
                                                             .Equals("aclientid")),
                It.IsAny<CancellationToken>()));
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
            codeChallengeMethod: OpenIdConnectCodeChallengeMethod.Plain);
        _repository.Setup(rep => rep.FindByAuthorizationCodeAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        var user = new EndUserWithMemberships
        {
            Id = "auserid",
            Classification = EndUserClassification.Person,
            Status = EndUserStatus.Registered
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
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(ats => ats.IssueTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "eyJanaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                },
                IdToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.OtherToken,
                    Value = "eyJanidtoken"
                }
            });

        var result = await _service.ExchangeCodeForTokensAsync(_caller.Object, "aclientid", "aclientsecret",
            "anauthorizationcode", "aredirecturi", "acodeverifier", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AccessToken.Should().Be("eyJadecryptedvalue");
        result.Value.TokenType.Should().Be(OAuth2TokenType.Bearer);
        result.Value.RefreshToken.Should().Be("eyJadecryptedvalue");
        result.Value.IdToken.Should().Be("eyJadecryptedvalue");
        _oauth2ClientService.Verify(ocs =>
            ocs.VerifyClientAsync(_caller.Object, "aclientid", "aclientsecret", It.IsAny<CancellationToken>()));
        _repository.Verify(rep =>
            rep.FindByAuthorizationCodeAsync("aclientid".ToId(), "anauthorizationcode",
                It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.SaveAsync(It.Is<OpenIdConnectAuthorizationRoot>(root =>
            root.AuthorizationCode == Optional<string>.None
            && root.AuthorizationExpiresAt == Optional<DateTime>.None
            && root.CodeChallenge.Value == "acodeverifier"
            && root.CodeChallengeMethod.Value == OpenIdConnectCodeChallengeMethod.Plain
            && root.Nonce.Value == "anonce"
            && root.RedirectUri.Value == "aredirecturi"
            && root.Scopes.Value.Items.SequenceEqual(new List<string> { OpenIdConnectConstants.Scopes.OpenId })
            && root.CodeExchangedAt.Value.IsNear(DateTime.UtcNow)
            && root.AccessToken.Value.DigestValue == "adigestvalue"
            && root.AccessToken.Value.ExpiresOn == expiresOn
            && root.RefreshToken.Value.DigestValue == "adigestvalue"
            && root.RefreshToken.Value.ExpiresOn == expiresOn
        ), It.IsAny<CancellationToken>()));
        _authTokensService.Verify(ats =>
            ats.IssueTokensAsync(_caller.Object, "auserid",
                It.Is<IReadOnlyList<string>>(scopes =>
                    scopes.SequenceEqual(new List<string> { OpenIdConnectConstants.Scopes.OpenId })),
                It.Is<Dictionary<string, object>>(dic => dic.ContainsKey(AuthenticationConstants.Claims.ForNonce) &&
                                                         dic[AuthenticationConstants.Claims.ForNonce].Equals("anonce")
                                                         && dic.ContainsKey(AuthenticationConstants.Claims.ForClientId)
                                                         &&
                                                         dic[AuthenticationConstants.Claims.ForClientId]
                                                             .Equals("aclientid")),
                It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetMembershipsPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.Is<ICallerContext>(cc =>
                cc.CallerId == CallerConstants.MaintenanceAccountUserId
            ), "auserid", It.IsAny<CancellationToken>()));
        _recorder.Verify(rec => rec.AuditAgainst(It.IsAny<ICallContext>(), "auserid",
            Audits.NativeIdentityServerOpenIdConnectService_Authorization_ExchangedCode,
            It.IsAny<string>(), It.IsAny<object[]>()));
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndClientVerificationFails_ThenReturnsError()
    {
        _oauth2ClientService.Setup(ocs => ocs.VerifyClientAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Validation("anerror"));

        var result = await _service.RefreshTokenAsync(_caller.Object, "aclientid", "aclientsecret",
            "arefreshtoken", "ascope", CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation,
            Resources.NativeIdentityServerOpenIdConnectService_ExchangeCodeForTokens_UnknownClient);
        _repository.Verify(rep =>
                rep.FindByAuthorizationCodeAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
            Times.Never);
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndAuthorizationNotFound_ThenReturnsError()
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
        _repository.Setup(rep => rep.FindByRefreshTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<OpenIdConnectAuthorizationRoot>.None);

        var result = await _service.RefreshTokenAsync(_caller.Object, "aclientid", "aclientsecret",
            "arefreshtoken", "ascope", CancellationToken.None);

        result.Should().BeError(ErrorCode.Validation,
            Resources.NativeIdentityServerOpenIdConnectService_RefreshToken_UnknownRefreshToken);
        _oauth2ClientService.Verify(ocs =>
            ocs.VerifyClientAsync(_caller.Object, "aclientid", "aclientsecret", It.IsAny<CancellationToken>()));
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest("arefreshtoken"));
        _repository.Verify(rep =>
            rep.FindByRefreshTokenDigestAsync("aclientid".ToId(), "adigestvalue", It.IsAny<CancellationToken>()));
        _authTokensService.Verify(ats =>
            ats.RefreshTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndUserNotAPerson_ThenReturnsError()
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
        var authorization = await CreateAExchangedAuthorizationAsync(AuthTokens.Create([
            AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
        ]).Value);
        _repository.Setup(rep => rep.FindByRefreshTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        var user = new EndUser
        {
            Id = "auserid",
            Classification = EndUserClassification.Machine,
            Status = EndUserStatus.Registered,
            Access = EndUserAccess.Enabled
        };
        _endUsersService.Setup(eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.RefreshTokenAsync(_caller.Object, "aclientid", "aclientsecret",
            "arefreshtoken", $"{OpenIdConnectConstants.Scopes.OpenId}", CancellationToken.None);

        result.Should().BeError(ErrorCode.ForbiddenAccess);
        _oauth2ClientService.Verify(ocs =>
            ocs.VerifyClientAsync(_caller.Object, "aclientid", "aclientsecret", It.IsAny<CancellationToken>()));
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest("arefreshtoken"));
        _repository.Verify(rep =>
            rep.FindByRefreshTokenDigestAsync("aclientid".ToId(), "adigestvalue", It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "anid", It.IsAny<CancellationToken>()));
        _repository.Verify(
            rep => rep.SaveAsync(It.IsAny<OpenIdConnectAuthorizationRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _authTokensService.Verify(ats =>
            ats.RefreshTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndUserNotRegistered_ThenReturnsError()
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
        var authorization = await CreateAExchangedAuthorizationAsync(AuthTokens.Create([
            AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
        ]).Value);
        _repository.Setup(rep => rep.FindByRefreshTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        var user = new EndUser
        {
            Id = "auserid",
            Classification = EndUserClassification.Person,
            Status = EndUserStatus.Unregistered,
            Access = EndUserAccess.Enabled
        };
        _endUsersService.Setup(eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.RefreshTokenAsync(_caller.Object, "aclientid", "aclientsecret",
            "arefreshtoken", $"{OpenIdConnectConstants.Scopes.OpenId}", CancellationToken.None);

        result.Should().BeError(ErrorCode.ForbiddenAccess);
        _oauth2ClientService.Verify(ocs =>
            ocs.VerifyClientAsync(_caller.Object, "aclientid", "aclientsecret", It.IsAny<CancellationToken>()));
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest("arefreshtoken"));
        _repository.Verify(rep =>
            rep.FindByRefreshTokenDigestAsync("aclientid".ToId(), "adigestvalue", It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "anid", It.IsAny<CancellationToken>()));
        _repository.Verify(
            rep => rep.SaveAsync(It.IsAny<OpenIdConnectAuthorizationRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _authTokensService.Verify(ats =>
            ats.RefreshTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndUserSuspended_ThenReturnsEntityLocked()
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
        var authorization = await CreateAExchangedAuthorizationAsync(AuthTokens.Create([
            AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
        ]).Value);
        _repository.Setup(rep => rep.FindByRefreshTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        var user = new EndUser
        {
            Id = "auserid",
            Classification = EndUserClassification.Person,
            Status = EndUserStatus.Registered,
            Access = EndUserAccess.Suspended
        };
        _endUsersService.Setup(eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.RefreshTokenAsync(_caller.Object, "aclientid", "aclientsecret",
            "arefreshtoken", $"{OpenIdConnectConstants.Scopes.OpenId}", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityLocked,
            Resources.NativeIdentityServerOpenIdConnectService_AccountSuspended);
        _recorder.Verify(rec => rec.AuditAgainst(It.IsAny<ICallContext>(), "anid".ToId(),
            Audits.NativeIdentityServerOpenIdConnectService_RefreshToken_AccountSuspended,
            "User {Id} tried to refresh token with {Provider} with a suspended account", "anid".ToId(),
            NativeIdentityServerOpenIdConnectService.ProviderName));
        _oauth2ClientService.Verify(ocs =>
            ocs.VerifyClientAsync(_caller.Object, "aclientid", "aclientsecret", It.IsAny<CancellationToken>()));
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest("arefreshtoken"));
        _repository.Verify(rep =>
            rep.FindByRefreshTokenDigestAsync("aclientid".ToId(), "adigestvalue", It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "anid", It.IsAny<CancellationToken>()));
        _repository.Verify(
            rep => rep.SaveAsync(It.IsAny<OpenIdConnectAuthorizationRoot>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _authTokensService.Verify(ats =>
            ats.RefreshTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenRefreshTokenAsync_ThenRefreshesTokens()
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
        var authorization = await CreateAExchangedAuthorizationAsync(AuthTokens.Create([
            AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
        ]).Value);
        _repository.Setup(rep => rep.FindByRefreshTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        var user = new EndUser
        {
            Id = "auserid",
            Classification = EndUserClassification.Person,
            Status = EndUserStatus.Registered,
            Access = EndUserAccess.Enabled
        };
        _endUsersService.Setup(eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        var expiresOn = DateTime.UtcNow;
        _authTokensService.Setup(ats => ats.RefreshTokensAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthenticateTokens
            {
                UserId = "auserid",
                AccessToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.AccessToken,
                    Value = "eyJanaccesstoken"
                },
                RefreshToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.RefreshToken,
                    Value = "arefreshtoken"
                },
                IdToken = new AuthenticationToken
                {
                    ExpiresOn = expiresOn,
                    Type = TokenType.OtherToken,
                    Value = "eyJanidtoken"
                }
            });

        var result = await _service.RefreshTokenAsync(_caller.Object, "aclientid", "aclientsecret",
            "arefreshtoken", $"{OpenIdConnectConstants.Scopes.OpenId}", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.AccessToken.Should().Be("eyJadecryptedvalue");
        result.Value.TokenType.Should().Be(OAuth2TokenType.Bearer);
        result.Value.RefreshToken.Should().Be("eyJadecryptedvalue");
        result.Value.IdToken.Should().Be("eyJadecryptedvalue");
        _oauth2ClientService.Verify(ocs =>
            ocs.VerifyClientAsync(_caller.Object, "aclientid", "aclientsecret", It.IsAny<CancellationToken>()));
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest("arefreshtoken"));
        _repository.Verify(rep =>
            rep.FindByRefreshTokenDigestAsync("aclientid".ToId(), "adigestvalue", It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "anid", It.IsAny<CancellationToken>()));
        _repository.Verify(rep => rep.SaveAsync(It.Is<OpenIdConnectAuthorizationRoot>(root =>
            root.AuthorizationCode == Optional<string>.None
            && root.AuthorizationExpiresAt == Optional<DateTime>.None
            && root.CodeChallenge == Optional<string>.None
            && root.CodeChallengeMethod.Value == OpenIdConnectCodeChallengeMethod.Plain
            && root.Nonce.Value == "anonce"
            && root.RedirectUri.Value == "aredirecturi"
            && root.Scopes.Value.Items.SequenceEqual(new List<string> { OpenIdConnectConstants.Scopes.OpenId })
            && root.CodeExchangedAt.Value.IsNear(DateTime.UtcNow)
            && root.AccessToken.Value.DigestValue == "adigestvalue"
            && root.AccessToken.Value.ExpiresOn == expiresOn
            && root.RefreshToken.Value.DigestValue == "adigestvalue"
            && root.RefreshToken.Value.ExpiresOn == expiresOn
            && root.LastRefreshedAt.Value.IsNear(DateTime.UtcNow)
        ), It.IsAny<CancellationToken>()));
        _authTokensService.Verify(ats =>
            ats.RefreshTokensAsync(_caller.Object, "arefreshtoken", It.Is<IReadOnlyList<string>>(scopes =>
                    scopes.SequenceEqual(new List<string> { $"{OpenIdConnectConstants.Scopes.OpenId}" })),
                It.Is<Dictionary<string, object>>(dic => dic.ContainsKey(AuthenticationConstants.Claims.ForNonce) &&
                                                         dic[AuthenticationConstants.Claims.ForNonce].Equals("anonce")
                                                         && dic.ContainsKey(AuthenticationConstants.Claims.ForClientId)
                                                         &&
                                                         dic[AuthenticationConstants.Claims.ForClientId]
                                                             .Equals("aclientid")),
                It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenGetUserInfoAsyncAndNotAuthenticated_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(false);

        var result = await _service.GetUserInfoAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        result.Error.AdditionalData.Should().ContainKey(NativeIdentityServerOpenIdConnectService.AuthErrorProviderName);
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest(It.IsAny<string>()), Times.Never);
        _repository.Verify(rep => rep.FindByAccessTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenGetUserInfoAsyncAndNoAuthorization_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _caller.Setup(cc => cc.Authorization)
            .Returns(Optional<ICallerContext.CallerAuthorization>.None);

        var result = await _service.GetUserInfoAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        result.Error.AdditionalData.Should().ContainKey(NativeIdentityServerOpenIdConnectService.AuthErrorProviderName);
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest(It.IsAny<string>()), Times.Never);
        _repository.Verify(rep => rep.FindByAccessTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenGetUserInfoAsyncAndNoAuthorizationToken_ThenReturnsError()
    {
        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _caller.Setup(cc => cc.Authorization)
            .Returns(new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.Token,
                Optional<string>.None).ToOptional());

        var result = await _service.GetUserInfoAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
        result.Error.AdditionalData.Should().ContainKey(NativeIdentityServerOpenIdConnectService.AuthErrorProviderName);
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest(It.IsAny<string>()), Times.Never);
        _repository.Verify(rep => rep.FindByAccessTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenGetUserInfoAsyncAndAuthorizationNotFound_ThenReturnsError()
    {
        SetupAuthenticatedCaller();
        _repository.Setup(rep => rep.FindByAccessTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<OpenIdConnectAuthorizationRoot>.None);

        var result = await _service.GetUserInfoAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.ForbiddenAccess);
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest(It.IsAny<string>()));
        _repository.Verify(rep => rep.FindByAccessTokenDigestAsync("auserid".ToId(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()));
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenGetUserInfoAsyncAndNotExchanged_ThenReturnsError()
    {
        SetupAuthenticatedCaller();
        var authorization = CreateAuthorizedAuthorization();
        _repository.Setup(rep => rep.FindByAccessTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        _oauth2ClientService.Setup(ocs => ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _service.GetUserInfoAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.ForbiddenAccess);
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest(It.IsAny<string>()));
        _repository.Verify(rep => rep.FindByAccessTokenDigestAsync("auserid".ToId(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()));
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenGetUserInfoAsyncAndNotConsented_ThenReturnsError()
    {
        SetupAuthenticatedCaller();
        var authorization = await CreateAExchangedAuthorizationAsync(AuthTokens.Create([
            AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
        ]).Value);
        _repository.Setup(rep => rep.FindByAccessTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        _oauth2ClientService.Setup(ocs => ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _service.GetUserInfoAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.ForbiddenAccess);
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest(It.IsAny<string>()));
        _repository.Verify(rep => rep.FindByAccessTokenDigestAsync("auserid".ToId(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()));
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(_caller.Object, "aclientid", "auserid",
                OpenIdConnectConstants.Scopes.OpenId, It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenGetUserInfoAsyncAndUserNotPerson_ThenReturnsError()
    {
        SetupAuthenticatedCaller();
        var authorization = await CreateAExchangedAuthorizationAsync(AuthTokens.Create([
            AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
        ]).Value);
        _repository.Setup(rep => rep.FindByAccessTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        _oauth2ClientService.Setup(ocs => ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var user = new EndUser
        {
            Id = "auserid",
            Classification = EndUserClassification.Machine,
            Status = EndUserStatus.Registered,
            Access = EndUserAccess.Enabled
        };
        _endUsersService.Setup(eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.GetUserInfoAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.ForbiddenAccess);
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest(It.IsAny<string>()));
        _repository.Verify(rep => rep.FindByAccessTokenDigestAsync("auserid".ToId(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()));
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(_caller.Object, "aclientid", "auserid",
                OpenIdConnectConstants.Scopes.OpenId, It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenGetUserInfoAsyncAndUserNotRegistered_ThenReturnsError()
    {
        SetupAuthenticatedCaller();
        var authorization = await CreateAExchangedAuthorizationAsync(AuthTokens.Create([
            AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
        ]).Value);
        _repository.Setup(rep => rep.FindByAccessTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        _oauth2ClientService.Setup(ocs => ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var user = new EndUser
        {
            Id = "auserid",
            Classification = EndUserClassification.Person,
            Status = EndUserStatus.Unregistered,
            Access = EndUserAccess.Enabled
        };
        _endUsersService.Setup(eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.GetUserInfoAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.ForbiddenAccess);
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest(It.IsAny<string>()));
        _repository.Verify(rep => rep.FindByAccessTokenDigestAsync("auserid".ToId(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()));
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(_caller.Object, "aclientid", "auserid",
                OpenIdConnectConstants.Scopes.OpenId, It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenGetUserInfoAsyncAndUserSuspended_ThenReturnsEntityLocked()
    {
        SetupAuthenticatedCaller();
        var authorization = await CreateAExchangedAuthorizationAsync(AuthTokens.Create([
            AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
        ]).Value);
        _repository.Setup(rep => rep.FindByAccessTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        _oauth2ClientService.Setup(ocs => ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var user = new EndUser
        {
            Id = "auserid",
            Classification = EndUserClassification.Person,
            Status = EndUserStatus.Registered,
            Access = EndUserAccess.Suspended
        };
        _endUsersService.Setup(eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _service.GetUserInfoAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityLocked,
            Resources.NativeIdentityServerOpenIdConnectService_AccountSuspended);
        _recorder.Verify(rec => rec.AuditAgainst(It.IsAny<ICallContext>(), "auserid",
            Audits.NativeIdentityServerOpenIdConnectService_UserInfo_AccountSuspended,
            "User {Id} tried to access user info with {Provider} with a suspended account", "auserid",
            NativeIdentityServerOpenIdConnectService.ProviderName));
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest(It.IsAny<string>()));
        _repository.Verify(rep => rep.FindByAccessTokenDigestAsync("auserid".ToId(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()));
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(_caller.Object, "aclientid", "auserid",
                OpenIdConnectConstants.Scopes.OpenId, It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenGetUserInfoAsyncWithNoScopes_ThenReturnsUserInfo()
    {
        SetupAuthenticatedCaller();
        var authorization = await CreateAExchangedAuthorizationAsync(AuthTokens.Create([
            AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.OtherToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
        ]).Value);
        _repository.Setup(rep => rep.FindByAccessTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        _oauth2ClientService.Setup(ocs => ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var user = new EndUser
        {
            Id = "auserid",
            Classification = EndUserClassification.Person,
            Status = EndUserStatus.Registered,
            Access = EndUserAccess.Enabled
        };
        _endUsersService.Setup(eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userProfilesService.Setup(ups => ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = "auserid",
                UserId = "auserid",
                DisplayName = "adisplayname",
                Name = new PersonName
                {
                    FirstName = "afirstname",
                    LastName = "alastname"
                },
                Address = new ProfileAddress
                {
                    Line1 = "aline1",
                    CountryCode = "acountrycode"
                },
                EmailAddress = "auser@company.com",
                PhoneNumber = "aphonenumber",
                Timezone = "atimezone",
                AvatarUrl = "anavatarurl"
            });

        var result = await _service.GetUserInfoAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Sub.Should().Be("auserid");
        result.Value.Name.Should().BeNull();
        result.Value.Email.Should().BeNull();
        result.Value.EmailVerified.Should().BeNull();
        result.Value.GivenName.Should().BeNull();
        result.Value.FamilyName.Should().BeNull();
        result.Value.Locale.Should().BeNull();
        result.Value.Picture.Should().BeNull();
        result.Value.ZoneInfo.Should().BeNull();
        result.Value.PhoneNumber.Should().BeNull();
        result.Value.PhoneNumberVerified.Should().BeNull();
        result.Value.Address.Should().BeNull();
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest(It.IsAny<string>()));
        _repository.Verify(rep => rep.FindByAccessTokenDigestAsync("auserid".ToId(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()));
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(_caller.Object, "aclientid", "auserid",
                OpenIdConnectConstants.Scopes.OpenId, It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenGetUserInfoAsyncWithOnlyProfileScope_ThenReturnsUserInfo()
    {
        SetupAuthenticatedCaller();
        var authorization = await CreateAExchangedAuthorizationAsync(AuthTokens.Create([
                AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
                AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
                AuthToken.Create(AuthTokenType.OtherToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
            ]).Value,
            [OpenIdConnectConstants.Scopes.OpenId, OAuth2Constants.Scopes.Profile]);
        _repository.Setup(rep => rep.FindByAccessTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        _oauth2ClientService.Setup(ocs => ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var user = new EndUser
        {
            Id = "auserid",
            Classification = EndUserClassification.Person,
            Status = EndUserStatus.Registered,
            Access = EndUserAccess.Enabled
        };
        _endUsersService.Setup(eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userProfilesService.Setup(ups => ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = "auserid",
                UserId = "auserid",
                DisplayName = "adisplayname",
                Name = new PersonName
                {
                    FirstName = "afirstname",
                    LastName = "alastname"
                },
                Address = new ProfileAddress
                {
                    Line1 = "aline1",
                    CountryCode = "acountrycode"
                },
                EmailAddress = "auser@company.com",
                PhoneNumber = "aphonenumber",
                Timezone = "atimezone",
                AvatarUrl = "anavatarurl"
            });

        var result = await _service.GetUserInfoAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Sub.Should().Be("auserid");
        result.Value.Name.Should().Be("afirstname alastname");
        result.Value.Email.Should().BeNull();
        result.Value.EmailVerified.Should().BeNull();
        result.Value.GivenName.Should().Be("afirstname");
        result.Value.FamilyName.Should().Be("alastname");
        result.Value.Locale.Should().BeNull();
        result.Value.Picture.Should().Be("anavatarurl");
        result.Value.ZoneInfo.Should().Be("atimezone");
        result.Value.PhoneNumber.Should().BeNull();
        result.Value.PhoneNumberVerified.Should().BeNull();
        result.Value.Address.Should().BeNull();
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest(It.IsAny<string>()));
        _repository.Verify(rep => rep.FindByAccessTokenDigestAsync("auserid".ToId(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()));
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(_caller.Object, "aclientid", "auserid",
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile}",
                It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenGetUserInfoAsyncWithAllScopes_ThenReturnsUserInfo()
    {
        SetupAuthenticatedCaller();
        var authorization = await CreateAExchangedAuthorizationAsync(AuthTokens.Create([
                AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
                AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
                AuthToken.Create(AuthTokenType.OtherToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
            ]).Value,
            [
                OpenIdConnectConstants.Scopes.OpenId, OAuth2Constants.Scopes.Profile, OAuth2Constants.Scopes.Email,
                OAuth2Constants.Scopes.Phone, OAuth2Constants.Scopes.Address
            ]);
        _repository.Setup(rep => rep.FindByAccessTokenDigestAsync(It.IsAny<Identifier>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(authorization.ToOptional());
        _oauth2ClientService.Setup(ocs => ocs.HasClientConsentedUserAsync(It.IsAny<ICallerContext>(),
                It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var user = new EndUser
        {
            Id = "auserid",
            Classification = EndUserClassification.Person,
            Status = EndUserStatus.Registered,
            Access = EndUserAccess.Enabled
        };
        _endUsersService.Setup(eus => eus.GetUserPrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _userProfilesService.Setup(ups => ups.GetProfilePrivateAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UserProfile
            {
                Id = "auserid",
                UserId = "auserid",
                DisplayName = "adisplayname",
                Name = new PersonName
                {
                    FirstName = "afirstname",
                    LastName = "alastname"
                },
                Address = new ProfileAddress
                {
                    Line1 = "aline1",
                    CountryCode = "acountrycode"
                },
                EmailAddress = "auser@company.com",
                PhoneNumber = "aphonenumber",
                Timezone = "atimezone",
                AvatarUrl = "anavatarurl"
            });

        var result = await _service.GetUserInfoAsync(_caller.Object, "auserid", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Sub.Should().Be("auserid");
        result.Value.Name.Should().Be("afirstname alastname");
        result.Value.Email.Should().Be("auser@company.com");
        result.Value.EmailVerified.Should().BeTrue();
        result.Value.GivenName.Should().Be("afirstname");
        result.Value.FamilyName.Should().Be("alastname");
        result.Value.Locale.Should().BeNull();
        result.Value.Picture.Should().Be("anavatarurl");
        result.Value.ZoneInfo.Should().Be("atimezone");
        result.Value.PhoneNumber.Should().Be("aphonenumber");
        result.Value.PhoneNumberVerified.Should().BeFalse();
        result.Value.Address!.Line1.Should().Be("aline1");
        result.Value.Address.CountryCode.Should().Be("acountrycode");
        _tokensService.Verify(ats =>
            ats.CreateTokenDigest(It.IsAny<string>()));
        _repository.Verify(rep => rep.FindByAccessTokenDigestAsync("auserid".ToId(), It.IsAny<string>(),
            It.IsAny<CancellationToken>()));
        _oauth2ClientService.Verify(ocs =>
            ocs.HasClientConsentedUserAsync(_caller.Object, "aclientid", "auserid",
                $"{OpenIdConnectConstants.Scopes.OpenId} {OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email} {OAuth2Constants.Scopes.Phone} {OAuth2Constants.Scopes.Address}",
                It.IsAny<CancellationToken>()));
        _endUsersService.Verify(eus =>
            eus.GetUserPrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
        _userProfilesService.Verify(ups =>
            ups.GetProfilePrivateAsync(_caller.Object, "auserid", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenGetJsonWebKeySetAsync_ThenReturnsKeySet()
    {
        var result = await _service.GetJsonWebKeySetAsync(_caller.Object, CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Keys.Count.Should().Be(1);
        result.Value.Keys[0].Kty.Should().Be("RSA");
        result.Value.Keys[0].Use.Should().Be("sig");
        result.Value.Keys[0].Kid.Should().Be("c52SNF2r");
        result.Value.Keys[0].Alg.Should().Be("RS256");
        result.Value.Keys[0].N.Should().Be(
            "oZlYKp93rSsf5ZO1Xzj857bqzASga+L9vfw3qAjEl3NqOOTILvtS+4Sw+IUZ0qv4xRTNXZmaLPy4fwLByCZX496y0buJ8ouvevR7etYFNn9NIKJcphV6jRyHFG6YgUejHzcwdSyhKuc9kPEjuOGhjSs1+94+VJmrYqUjFDgjMOl/GqVQhHwwQzbWiZD6gJiICXpIUpBo0K65TmwGBgm/Zj5ImZZI0aKmbLY4aod5LiTO8JCqzE9KphH/XBb7oXYlURQ8DHLFnfsIwO7VTD6qS6LiBY+9Vv6YFMJ4oinULOJJt9EJgvbCzbvSGiY37k6y4Dn2jbmwcDjE2J7z0muNmQ==");
        result.Value.Keys[0].E.Should().Be("AQAB");
    }

    private OpenIdConnectAuthorizationRoot CreateAuthorization(string clientId = "aclientid", string userId = "auserid")
    {
        return OpenIdConnectAuthorizationRoot.Create(_recorder.Object, _idFactory.Object, _encryptionService.Object,
            _tokensService.Object,
            clientId.ToId(), userId.ToId()).Value;
    }

    private OpenIdConnectAuthorizationRoot CreateAuthorizedAuthorization(string clientId = "aclientid",
        string userId = "auserid", string redirectUri = "aredirecturi", string? nonce = "anonce",
        string? codeChallenge = null, OpenIdConnectCodeChallengeMethod? codeChallengeMethod = null,
        List<string>? scopes = null)
    {
        var authorization = CreateAuthorization(clientId, userId);
        var scopes2 = OAuth2Scopes.Create(scopes.NotExists()
            ? [OpenIdConnectConstants.Scopes.OpenId]
            : scopes).Value;
        authorization.AuthorizeCode(redirectUri, redirectUri, scopes2, nonce.ToOptional(),
            codeChallenge.ToOptional(), codeChallengeMethod.ToOptional());
        return authorization;
    }

    private async Task<OpenIdConnectAuthorizationRoot> CreateAExchangedAuthorizationAsync(AuthTokens tokens,
        List<string>? scopes = null)
    {
        var authorization = CreateAuthorizedAuthorization(scopes: scopes);
        await authorization.ExchangeCodeAsync("aredirecturi", null,
            _ => Task.FromResult<Result<AuthTokens, Error>>(tokens));
        return authorization;
    }

    private static string CreateJwtToken()
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("a-secret-key-for-testing-purposes-only-that-is-long-enough");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([]),
            Audience = "anaudience",
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials =
                new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private void SetupAuthenticatedCaller()
    {
        var jwtToken = CreateJwtToken();

        _caller.Setup(cc => cc.IsAuthenticated)
            .Returns(true);
        _caller.Setup(cc => cc.Authorization)
            .Returns(new ICallerContext.CallerAuthorization(ICallerContext.AuthorizationMethod.Token, jwtToken));
    }
}