using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Identities.OpenIdConnect.Authorizations;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Services.Shared;
using Domain.Shared.Identities;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace IdentityDomain.UnitTests;

[Trait("Category", "Unit")]
public class OidcAuthorizationRootSpec
{
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<ITokensService> _tokensService;

    public OidcAuthorizationRootSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateOAuthAuthorizationCode(It.IsAny<string>()))
            .Returns("anauthorizationcode");
    }

    [Fact]
    public void WhenCreate_ThenCreated()
    {
        var result =
            OidcAuthorizationRoot.Create(_recorder.Object, _idFactory.Object, _tokensService.Object, "aclientid".ToId(),
                "auserid".ToId());

        result.Should().BeSuccess();
        result.Value.ClientId.Should().Be("aclientid".ToId());
        result.Value.UserId.Should().Be("auserid".ToId());
        result.Value.AuthorizationCode.Should().BeNone();
        result.Value.AuthorizationExpiresAt.Should().BeNone();
        result.Value.CodeChallenge.Should().BeNone();
        result.Value.CodeChallengeMethod.Should().BeNone();
        result.Value.Nonce.Should().BeNone();
        result.Value.RedirectUri.Should().BeNone();
        result.Value.Scopes.Should().BeNone();
        result.Value.IsAuthorized.Should().BeFalse();
        result.Value.IsExchanged.Should().BeFalse();
        result.Value.Events.Last().Should().BeOfType<Created>();
    }

    [Fact]
    public void WhenEnsureInvariantsAndMissingUserId_ThenReturnsError()
    {
        var authorization = OidcAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _tokensService.Object, "aclientid".ToId(), Identifier.Empty())
            .Value;

        var result = authorization.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.OidcAuthorizationRoot_MissingUserId);
    }

    [Fact]
    public void WhenEnsureInvariantsAndMissingClientId_ThenReturnsError()
    {
        var authorization = OidcAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _tokensService.Object, Identifier.Empty(), "auserid".ToId())
            .Value;

        var result = authorization.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.OidcAuthorizationRoot_MissingClientId);
    }

    [Fact]
    public void WhenAuthorizeCodeAndClientRedirectUriIsMissing_ThenReturnsError()
    {
        var authorization = OidcAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _tokensService.Object, "aclientid".ToId(), "auserid".ToId())
            .Value;

        var result = authorization.AuthorizeCode(Optional<string>.None, "aredirecturi", OAuth2Scopes.Empty,
            Optional<string>.None, Optional<string>.None, Optional<OAuth2CodeChallengeMethod>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OidcAuthorizationRoot_GenerateCode_MissingClientRedirectUri);
    }

    [Fact]
    public void WhenAuthorizeCodeAndClientRedirectUriIsNotSame_ThenReturnsError()
    {
        var authorization = OidcAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _tokensService.Object, "aclientid".ToId(), "auserid".ToId())
            .Value;

        var result = authorization.AuthorizeCode("aredirecturi", "anotherredirecturi", OAuth2Scopes.Empty,
            Optional<string>.None, Optional<string>.None, Optional<OAuth2CodeChallengeMethod>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OidcAuthorizationRoot_GenerateCode_MismatchedClientRedirectUri);
    }

    [Fact]
    public void WhenAuthorizeCodeAndScopeNotIncludeOidc_ThenReturnsError()
    {
        var authorization = OidcAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _tokensService.Object, "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]).Value;

        var result = authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            Optional<string>.None, Optional<string>.None, Optional<OAuth2CodeChallengeMethod>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OidcAuthorizationRoot_GenerateCode_MissingOpenIdScope);
    }

    [Fact]
    public void WhenAuthorizeCodeAndCodeChallengeButNoMethod_ThenReturnsError()
    {
        var authorization = OidcAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _tokensService.Object, "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;

        var result = authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            Optional<string>.None, "acodechallenge", Optional<OAuth2CodeChallengeMethod>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OidcAuthorizationRoot_GenerateCode_CodeChallengeMissingMethod);
    }

    [Fact]
    public void WhenAuthorizeCode_ThenGeneratesCode()
    {
        var authorization = OidcAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _tokensService.Object, "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;

        var result = authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            "anonce", "acodechallenge", OAuth2CodeChallengeMethod.Plain);

        result.Should().BeSuccess();
        authorization.AuthorizationCode.Should().Be("aclientid:anauthorizationcode");
        authorization.AuthorizationExpiresAt.Should().BeNear(DateTime.UtcNow.AddMinutes(10));
        authorization.CodeChallenge.Should().Be("acodechallenge");
        authorization.CodeChallengeMethod.Should().Be(OAuth2CodeChallengeMethod.Plain);
        authorization.Nonce.Should().Be("anonce");
        authorization.RedirectUri.Should().Be("aredirecturi");
        authorization.Events.Last().Should().BeOfType<CodeAuthorized>();
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncAndNotAuthorized_ThenReturnsError()
    {
        var authorization = OidcAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _tokensService.Object, "aclientid".ToId(), "auserid".ToId())
            .Value;

        var result = await authorization.ExchangeCodeAsync("aredirecturi", null,
            _ => Task.FromResult(
                AuthTokens.Create(new List<Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken>())));

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OidcAuthorizationRoot_VerifyCode_NotConfigured);
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncAndCodeExpired_ThenReturnsError()
    {
        var authorization = OidcAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _tokensService.Object, "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            Optional<string>.None, Optional<string>.None, Optional<OAuth2CodeChallengeMethod>.None);

#if TESTINGONLY
        authorization.TestingOnly_ExpireAuthorizationCode();
#endif

        var result = await authorization.ExchangeCodeAsync("aredirecturi", null,
            _ => Task.FromResult(
                AuthTokens.Create(new List<Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken>())));

        result.Should().BeError(ErrorCode.Validation,
            Resources.OidcAuthorizationRoot_VerifyCode_ExpiredAuthorizationCode);
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncAndRedirectUriMismatch_ThenReturnsError()
    {
        var authorization = OidcAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _tokensService.Object, "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            Optional<string>.None, Optional<string>.None, Optional<OAuth2CodeChallengeMethod>.None);

        var result = await authorization.ExchangeCodeAsync("anotherredirecturi", null,
            _ => Task.FromResult(
                AuthTokens.Create(new List<Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken>())));

        result.Should().BeError(ErrorCode.Validation, Resources.OidcAuthorizationRoot_VerifyCode_MismatchedRedirectUri);
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncAndCodeVerifierMissing_ThenReturnsError()
    {
        var authorization = OidcAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _tokensService.Object, "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            Optional<string>.None, "acodechallenge", OAuth2CodeChallengeMethod.Plain);

        var result = await authorization.ExchangeCodeAsync("aredirecturi", null,
            _ => Task.FromResult(
                AuthTokens.Create(new List<Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken>())));

        result.Should().BeError(ErrorCode.Validation, Resources.OidcAuthorizationRoot_VerifyPkce_MissingCodeVerifier);
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncAndInvalidCodeVerifier_ThenReturnsError()
    {
        var authorization = OidcAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _tokensService.Object, "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            Optional<string>.None, "acodechallenge", OAuth2CodeChallengeMethod.Plain);

        var result = await authorization.ExchangeCodeAsync("aredirecturi", "aninvalidcodeverifier",
            _ => Task.FromResult(
                AuthTokens.Create(new List<Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken>())));

        result.Should().BeError(ErrorCode.Validation, Resources.OidcAuthorizationRoot_VerifyPkce_InvalidCodeVerifier);
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncWithoutPkce_ThenCodeExchanged()
    {
        var authorization = OidcAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _tokensService.Object, "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            "anonce", Optional<string>.None, Optional<OAuth2CodeChallengeMethod>.None);
        var tokens = AuthTokens.Create(new List<Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken>());

        var result = await authorization.ExchangeCodeAsync("aredirecturi", null,
            _ => Task.FromResult(tokens));

        result.Should().BeSuccess();
        result.Value.Should().Be(tokens.Value);
        authorization.Events.Last().Should().BeOfType<CodeExchanged>();
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncWithPkcePlain_ThenCodeExchanged()
    {
        var authorization = OidcAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _tokensService.Object, "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            "anonce", "acodechallenge", OAuth2CodeChallengeMethod.Plain);
        var tokens = AuthTokens.Create(new List<Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken>());

        var result = await authorization.ExchangeCodeAsync("aredirecturi", "acodechallenge",
            _ => Task.FromResult(tokens));

        result.Should().BeSuccess();
        result.Value.Should().Be(tokens.Value);
        authorization.Events.Last().Should().BeOfType<CodeExchanged>();
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncWithPkceS256_ThenCodeExchanged()
    {
        var authorization = OidcAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _tokensService.Object, "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        var codeChallenge = "WWHTYIjNclXxS69q1gerQ+eTlW5ab1YCpKTorurQ3zw=";
        var codeVerifier = "1234567890123456789012345678901234567890123";
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            "anonce", codeChallenge, OAuth2CodeChallengeMethod.S256);
        var tokens = AuthTokens.Create(new List<Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken>());

        var result = await authorization.ExchangeCodeAsync("aredirecturi", codeVerifier,
            _ => Task.FromResult(tokens));

        result.Should().BeSuccess();
        result.Value.Should().Be(tokens.Value);
        authorization.Events.Last().Should().BeOfType<CodeExchanged>();
    }
}