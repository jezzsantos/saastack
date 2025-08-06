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
public class OpenIdConnectAuthorizationRootSpec
{
    private readonly Mock<IEncryptionService> _encryptionService;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<ITokensService> _tokensService;

    public OpenIdConnectAuthorizationRootSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateOAuthorizationCodeDigest(It.IsAny<string>()))
            .Returns("anauthorizationcode");
        _tokensService.Setup(ts => ts.CreateTokenDigest(It.IsAny<string>()))
            .Returns("adigestvalue");
        _encryptionService = new Mock<IEncryptionService>();
        _encryptionService.Setup(es => es.Decrypt(It.IsAny<string>()))
            .Returns("adecryptedvalue");
    }

    [Fact]
    public void WhenCreate_ThenCreated()
    {
        var result =
            OpenIdConnectAuthorizationRoot.Create(_recorder.Object, _idFactory.Object, _encryptionService.Object,
                _tokensService.Object,
                "aclientid".ToId(),
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
        result.Value.IsExchangable.Should().BeFalse();
        result.Value.Events.Last().Should().BeOfType<Created>();
    }

    [Fact]
    public void WhenEnsureInvariantsAndMissingUserId_ThenReturnsError()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), Identifier.Empty())
            .Value;

        var result = authorization.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.OpenIdConnectAuthorizationRootMissingUserId);
    }

    [Fact]
    public void WhenEnsureInvariantsAndMissingClientId_ThenReturnsError()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                Identifier.Empty(), "auserid".ToId())
            .Value;

        var result = authorization.EnsureInvariants();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.OpenIdConnectAuthorizationRootMissingClientId);
    }

    [Fact]
    public void WhenAuthorizeCodeAndClientRedirectUriIsMissing_ThenReturnsError()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;

        var result = authorization.AuthorizeCode(Optional<string>.None, "aredirecturi", OAuth2Scopes.Empty,
            Optional<string>.None, Optional<string>.None, Optional<OpenIdConnectCodeChallengeMethod>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OpenIdConnectAuthorizationRootGenerateCode_MissingClientRedirectUri);
    }

    [Fact]
    public void WhenAuthorizeCodeAndClientRedirectUriIsNotSame_ThenReturnsError()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;

        var result = authorization.AuthorizeCode("aredirecturi", "anotherredirecturi", OAuth2Scopes.Empty,
            Optional<string>.None, Optional<string>.None, Optional<OpenIdConnectCodeChallengeMethod>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OpenIdConnectAuthorizationRootGenerateCode_MismatchedClientRedirectUri);
    }

    [Fact]
    public void WhenAuthorizeCodeAndScopeNotIncludeOidc_ThenReturnsError()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile]).Value;

        var result = authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            Optional<string>.None, Optional<string>.None, Optional<OpenIdConnectCodeChallengeMethod>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OpenIdConnectAuthorizationRootMissingOpenIdScope);
    }

    [Fact]
    public void WhenAuthorizeCodeAndCodeChallengeButNoMethod_ThenReturnsError()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;

        var result = authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            Optional<string>.None, "acodechallenge", Optional<OpenIdConnectCodeChallengeMethod>.None);

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OpenIdConnectAuthorizationRootGenerateCode_CodeChallengeMissingMethod);
    }

    [Fact]
    public void WhenAuthorizeCode_ThenGeneratesCode()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;

        var result = authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            "anonce", "acodechallenge", OpenIdConnectCodeChallengeMethod.Plain);

        result.Should().BeSuccess();
        authorization.AuthorizationCode.Should().Be("aclientid:anauthorizationcode");
        authorization.AuthorizationExpiresAt.Should().BeNear(DateTime.UtcNow.AddMinutes(10));
        authorization.CodeChallenge.Should().Be("acodechallenge");
        authorization.CodeChallengeMethod.Should().Be(OpenIdConnectCodeChallengeMethod.Plain);
        authorization.Nonce.Should().Be("anonce");
        authorization.RedirectUri.Should().Be("aredirecturi");
        authorization.Events.Last().Should().BeOfType<CodeAuthorized>();
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncAndNotAuthorized_ThenReturnsError()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;

        var result = await authorization.ExchangeCodeAsync("aredirecturi", null,
            _ => Task.FromResult(
                AuthTokens.Create(new List<Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken>())));

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OpenIdConnectAuthorizationRootVerifyCode_NotConfigured);
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncAndNotIsExchangable_ThenReturnsError()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            Optional<string>.None, Optional<string>.None, Optional<OpenIdConnectCodeChallengeMethod>.None);

#if TESTINGONLY
        authorization.TestingOnly_ExpireAuthorizationCode();
#endif

        var result = await authorization.ExchangeCodeAsync("aredirecturi", null,
            _ => Task.FromResult(
                AuthTokens.Create(new List<Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken>())));

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OpenIdConnectAuthorizationRootVerifyCode_ExpiredAuthorizationCode);
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncAndRedirectUriMismatch_ThenReturnsError()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            Optional<string>.None, Optional<string>.None, Optional<OpenIdConnectCodeChallengeMethod>.None);

        var result = await authorization.ExchangeCodeAsync("anotherredirecturi", null,
            _ => Task.FromResult(
                AuthTokens.Create(new List<Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken>())));

        result.Should().BeError(ErrorCode.Validation,
            Resources.OpenIdConnectAuthorizationRootVerifyCode_MismatchedRedirectUri);
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncAndCodeVerifierMissing_ThenReturnsError()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            Optional<string>.None, "acodechallenge", OpenIdConnectCodeChallengeMethod.Plain);

        var result = await authorization.ExchangeCodeAsync("aredirecturi", null,
            _ => Task.FromResult(
                AuthTokens.Create(new List<Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken>())));

        result.Should().BeError(ErrorCode.Validation,
            Resources.OpenIdConnectAuthorizationRootVerifyPkce_MissingCodeVerifier);
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncAndInvalidCodeVerifier_ThenReturnsError()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            Optional<string>.None, "acodechallenge", OpenIdConnectCodeChallengeMethod.Plain);

        var result = await authorization.ExchangeCodeAsync("aredirecturi", "aninvalidcodeverifier",
            _ => Task.FromResult(
                AuthTokens.Create(new List<Domain.Events.Shared.Identities.ProviderAuthTokens.AuthToken>())));

        result.Should().BeError(ErrorCode.Validation,
            Resources.OpenIdConnectAuthorizationRootVerifyPkce_InvalidCodeVerifier);
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncWithoutPkce_ThenCodeExchanged()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            "anonce", Optional<string>.None, Optional<OpenIdConnectCodeChallengeMethod>.None);
        var tokens = AuthTokens.Create([
            AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
        ]);

        var result = await authorization.ExchangeCodeAsync("aredirecturi", null,
            _ => Task.FromResult(tokens));

        result.Should().BeSuccess();
        result.Value.Should().Be(tokens.Value);
        authorization.Scopes.Should().Be(scopes);
        authorization.AccessToken.Value.DigestValue.Should().Be("adigestvalue");
        authorization.RefreshToken.Value.DigestValue.Should().Be("adigestvalue");
        authorization.Events.Last().Should().BeOfType<CodeExchanged>();
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncWithPkcePlain_ThenCodeExchanged()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            "anonce", "acodechallenge", OpenIdConnectCodeChallengeMethod.Plain);
        var tokens = AuthTokens.Create([
            AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
        ]);

        var result = await authorization.ExchangeCodeAsync("aredirecturi", "acodechallenge",
            _ => Task.FromResult(tokens));

        result.Should().BeSuccess();
        result.Value.Should().Be(tokens.Value);
        authorization.Scopes.Should().Be(scopes);
        authorization.AccessToken.Value.DigestValue.Should().Be("adigestvalue");
        authorization.RefreshToken.Value.DigestValue.Should().Be("adigestvalue");
        authorization.Events.Last().Should().BeOfType<CodeExchanged>();
    }

    [Fact]
    public async Task WhenExchangeCodeAsyncWithPkceS256_ThenCodeExchanged()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;
        var scopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        var codeChallenge = "WWHTYIjNclXxS69q1gerQ+eTlW5ab1YCpKTorurQ3zw=";
        var codeVerifier = "1234567890123456789012345678901234567890123";
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", scopes,
            "anonce", codeChallenge, OpenIdConnectCodeChallengeMethod.S256);
        var tokens = AuthTokens.Create([
            AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
        ]);

        var result = await authorization.ExchangeCodeAsync("aredirecturi", codeVerifier,
            _ => Task.FromResult(tokens));

        result.Should().BeSuccess();
        result.Value.Should().Be(tokens.Value);
        authorization.Scopes.Should().Be(scopes);
        authorization.AccessToken.Value.DigestValue.Should().Be("adigestvalue");
        authorization.RefreshToken.Value.DigestValue.Should().Be("adigestvalue");
        authorization.Events.Last().Should().BeOfType<CodeExchanged>();
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndNotRefreshableThenReturnsError()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;
        var originalScopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", originalScopes,
            "anonce", Optional<string>.None, Optional<OpenIdConnectCodeChallengeMethod>.None);
        var tokens = AuthTokens.Create([
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow).Value
        ]);

        var result = await authorization.RefreshTokenAsync(Optional<OAuth2Scopes>.None, _ => Task.FromResult(tokens));

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OpenIdConnectAuthorizationRootRefreshToken_RefreshForbidden);
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndScopesAreMoreThanPreviouslyAuthorized_ThenReturnsError()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;
        var originalScopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId]).Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", originalScopes,
            "anonce", Optional<string>.None, Optional<OpenIdConnectCodeChallengeMethod>.None);
        var tokens = AuthTokens.Create([
            AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
        ]);
        await authorization.ExchangeCodeAsync("aredirecturi", null, _ => Task.FromResult(tokens));
        var newScopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId, OAuth2Constants.Scopes.Profile])
            .Value;

        var result = await authorization.RefreshTokenAsync(newScopes, _ => Task.FromResult(tokens));

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OpenIdConnectAuthorizationRootRefreshToken_ScopesNotSubset);
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndScopesNotIncludeOpenIdThenReturnsError()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;
        var originalScopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId, OAuth2Constants.Scopes.Profile])
            .Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", originalScopes,
            "anonce", Optional<string>.None, Optional<OpenIdConnectCodeChallengeMethod>.None);
        var tokens = AuthTokens.Create([
            AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
        ]);
        await authorization.ExchangeCodeAsync("aredirecturi", null, _ => Task.FromResult(tokens));
        var newScopes = OAuth2Scopes.Create([OAuth2Constants.Scopes.Profile])
            .Value;

        var result = await authorization.RefreshTokenAsync(newScopes, _ => Task.FromResult(tokens));

        result.Should().BeError(ErrorCode.PreconditionViolation,
            Resources.OpenIdConnectAuthorizationRootMissingOpenIdScope);
    }

    [Fact]
    public async Task WhenRefreshTokenAsync_ThenRefreshed()
    {
        var authorization = OpenIdConnectAuthorizationRoot
            .Create(_recorder.Object, _idFactory.Object, _encryptionService.Object, _tokensService.Object,
                "aclientid".ToId(), "auserid".ToId())
            .Value;
        var originalScopes = OAuth2Scopes.Create([
            OpenIdConnectConstants.Scopes.OpenId, OAuth2Constants.Scopes.Profile, OAuth2Constants.Scopes.Email
        ]).Value;
        authorization.AuthorizeCode("aredirecturi", "aredirecturi", originalScopes,
            "anonce", Optional<string>.None, Optional<OpenIdConnectCodeChallengeMethod>.None);
        var tokens = AuthTokens.Create([
            AuthToken.Create(AuthTokenType.AccessToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value,
            AuthToken.Create(AuthTokenType.RefreshToken, "anencryptedvalue", DateTime.UtcNow.AddMinutes(1)).Value
        ]);
        await authorization.ExchangeCodeAsync("aredirecturi", null, _ => Task.FromResult(tokens));
        var newScopes = OAuth2Scopes.Create([OpenIdConnectConstants.Scopes.OpenId, OAuth2Constants.Scopes.Profile])
            .Value;

        var result = await authorization.RefreshTokenAsync(newScopes, _ => Task.FromResult(tokens));

        result.Should().BeSuccess();
        result.Value.Should().Be(tokens.Value);
        authorization.Scopes.Should().Be(newScopes);
        authorization.AccessToken.Value.DigestValue.Should().Be("adigestvalue");
        authorization.RefreshToken.Value.DigestValue.Should().Be("adigestvalue");
        authorization.Events.Last().Should().BeOfType<TokenRefreshed>();
    }
}