using Domain.Interfaces.Validations;
using FluentAssertions;
using Infrastructure.Shared.DomainServices;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Shared.UnitTests.DomainServices;

[Trait("Category", "Unit")]
public class TokensServiceSpec
{
    private readonly TokensService _service = new();

    [Fact]
    public void WhenCreateTokenForVerification_ThenReturnsRandomValue()
    {
        var result1 = _service.CreateRegistrationVerificationToken();
        var result2 = _service.CreateRegistrationVerificationToken();
        var result3 = _service.CreateRegistrationVerificationToken();

        result1.Should().MatchRegex(CommonValidations.RandomToken().Expression);
        result1.Should().NotBe(result2);
        result2.Should().NotBe(result3);
        result3.Should().NotBe(result1);
    }

    [Fact]
    public void WhenCreateTokenForPasswordReset_ThenReturnsRandomValue()
    {
        var result1 = _service.CreatePasswordResetToken();
        var result2 = _service.CreatePasswordResetToken();
        var result3 = _service.CreatePasswordResetToken();

        result1.Should().MatchRegex(CommonValidations.RandomToken().Expression);
        result1.Should().NotBe(result2);
        result2.Should().NotBe(result3);
        result3.Should().NotBe(result1);
    }

    [Fact]
    public void WhenCreateTokenForJwtRefresh_ThenReturnsRandomValue()
    {
        var result1 = _service.CreateJWTRefreshToken();
        var result2 = _service.CreateJWTRefreshToken();
        var result3 = _service.CreateJWTRefreshToken();

        result1.Should().MatchRegex(CommonValidations.RandomToken().Expression);
        result1.Should().NotBe(result2);
        result2.Should().NotBe(result3);
        result3.Should().NotBe(result1);
    }

    [Fact]
    public void WhenCreateApiKey_ThenReturnsNewRandomApiKey()
    {
        var result = _service.CreateAPIKey();

        result.Prefix.Should().Be(CommonValidations.APIKeys.ApiKeyPrefix);
        result.Token.Should().MatchRegex(CommonValidations.APIKeys.RandomToken(24).Expression);
        result.Key.Should().MatchRegex(CommonValidations.APIKeys.RandomToken().Expression);
        result.ApiKey.Should().StartWith(CommonValidations.APIKeys.ApiKeyPrefix)
            .And.Contain(CommonValidations.APIKeys.ApiKeyDelimiter);
    }

    [Fact]
    public void WhenParseApiKeyAndNotAnApiKey_ThenReturnsNone()
    {
        var result = _service.ParseApiKey("anotanapikey");

        result.Should().BeNone();
    }

    [Fact]
    public void WhenParseApiKey_ThenReturnsKey()
    {
        var keyToken = _service.CreateAPIKey();

        var result = _service.ParseApiKey(keyToken.ApiKey);

        result.Value.Prefix.Should().Be(keyToken.Prefix);
        result.Value.Token.Should().Be(keyToken.Token);
        result.Value.Key.Should().Be(keyToken.Key);
        result.Value.ApiKey.Should().Be(keyToken.ApiKey);
    }

    [Fact]
    public void WhenCreateOAuth2ClientSecret_ThenReturnsNewRandomValue()
    {
        var result1 = _service.CreateOAuth2ClientSecret();
        var result2 = _service.CreateOAuth2ClientSecret();
        var result3 = _service.CreateOAuth2ClientSecret();

        result1.Should().MatchRegex(CommonValidations.RandomToken().Expression);
        result1.Should().NotBe(result2);
        result2.Should().NotBe(result3);
        result3.Should().NotBe(result1);
    }

    [Fact]
    public void WhenCreateOAuthAuthorizationCode_ThenReturnsDeterministicHash()
    {
        var result1 = _service.CreateOAuthorizationCodeDigest("avalue");
        var result2 = _service.CreateOAuthorizationCodeDigest("avalue");
        var result3 = _service.CreateOAuthorizationCodeDigest("avalue");

        result1.Should().MatchRegex(CommonValidations.RandomToken().Expression);
        result1.Should().Be(result2);
        result2.Should().Be(result3);
        result3.Should().Be(result1);
    }
}