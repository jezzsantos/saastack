using Domain.Interfaces.Validations;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Shared.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class TokensServiceSpec
{
    private readonly TokensService _service = new();

    [Fact]
    public void WhenCreateTokenForVerification_ThenReturnsRandomValue()
    {
        var result1 = _service.CreateTokenForVerification();
        var result2 = _service.CreateTokenForVerification();
        var result3 = _service.CreateTokenForVerification();

        result1.Should().MatchRegex(CommonValidations.RandomToken().Expression);
        result1.Should().NotBe(result2);
        result2.Should().NotBe(result3);
        result3.Should().NotBe(result1);
    }

    [Fact]
    public void WhenCreateTokenForPasswordReset_ThenReturnsRandomValue()
    {
        var result1 = _service.CreateTokenForPasswordReset();
        var result2 = _service.CreateTokenForPasswordReset();
        var result3 = _service.CreateTokenForPasswordReset();

        result1.Should().MatchRegex(CommonValidations.RandomToken().Expression);
        result1.Should().NotBe(result2);
        result2.Should().NotBe(result3);
        result3.Should().NotBe(result1);
    }

    [Fact]
    public void WhenCreateTokenForJwtRefresh_ThenReturnsRandomValue()
    {
        var result1 = _service.CreateTokenForJwtRefresh();
        var result2 = _service.CreateTokenForJwtRefresh();
        var result3 = _service.CreateTokenForJwtRefresh();

        result1.Should().MatchRegex(CommonValidations.RandomToken().Expression);
        result1.Should().NotBe(result2);
        result2.Should().NotBe(result3);
        result3.Should().NotBe(result1);
    }

    [Fact]
    public void WhenCreateApiKey_ThenReturnsNewRandomApiKey()
    {
        var result = _service.CreateApiKey();

        result.Prefix.Should().Be(CommonValidations.APIKeys.ApiKeyPrefix);
        result.Token.Should().MatchRegex(CommonValidations.RandomToken(24).Expression);
        result.Key.Should().MatchRegex(CommonValidations.RandomToken().Expression);
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
        var keyToken = _service.CreateApiKey();

        var result = _service.ParseApiKey(keyToken.ApiKey);

        result.Value.Prefix.Should().Be(keyToken.Prefix);
        result.Value.Token.Should().Be(keyToken.Token);
        result.Value.Key.Should().Be(keyToken.Key);
        result.Value.ApiKey.Should().Be(keyToken.ApiKey);
    }
}