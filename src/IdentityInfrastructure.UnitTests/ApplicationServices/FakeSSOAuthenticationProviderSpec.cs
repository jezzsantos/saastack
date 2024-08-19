#if TESTINGONLY
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using FluentAssertions;
using IdentityInfrastructure.ApplicationServices;
using Infrastructure.Interfaces;
using Moq;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class FakeSSOAuthenticationProviderSpec
{
    private readonly Mock<ICallerContext> _caller;
    private readonly FakeSSOAuthenticationProvider _provider;

    public FakeSSOAuthenticationProviderSpec()
    {
        _caller = new Mock<ICallerContext>();
        _provider = new FakeSSOAuthenticationProvider();
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndNoAuthCode_ThenReturnsError()
    {
        await _provider.Invoking(x =>
                x.AuthenticateAsync(_caller.Object, string.Empty, "anemailaddress", CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.AnySSOAuthenticationProvider_MissingAuthCode);
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndNoUsername_ThenReturnsError()
    {
        var result =
            await _provider.AuthenticateAsync(_caller.Object, "1234567890", null, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.FakeSSOAuthenticationProvider_MissingUsername);
    }

    [Fact]
    public async Task WhenAuthenticateAsyncAndWrongAuthCode_ThenReturnsError()
    {
        var result =
            await _provider.AuthenticateAsync(_caller.Object, "awrongcode", null, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.FakeSSOAuthenticationProvider_MissingUsername);
    }

    [Fact]
    public async Task WhenAuthenticateAsync_ThenReturnsTokens()
    {
        var result =
            await _provider.AuthenticateAsync(_caller.Object, "1234567890", "anemailaddress", CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.Tokens.Count.Should().Be(1);
        result.Value.Tokens[0].Type.Should().Be(TokenType.AccessToken);
        result.Value.Tokens[0].Value.Should().NotBeNull();
        result.Value.Tokens[0].ExpiresOn.Should()
            .BeCloseTo(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry),
                TimeSpan.FromMinutes(1));
        result.Value.EmailAddress.Should().Be("anemailaddress");
        result.Value.FirstName.Should().Be("anemailaddress");
        result.Value.LastName.Should().Be("asurname");
        result.Value.Timezone.Should().Be(Timezones.Default);
        result.Value.CountryCode.Should().Be(CountryCodes.Default);
    }

    [Fact]
    public async Task WhenRefreshTokenAsyncAndNoRefreshToken_ThenReturnsError()
    {
        await _provider.Invoking(x => x.RefreshTokenAsync(_caller.Object, string.Empty, CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.AnySSOAuthenticationProvider_MissingRefreshToken);
    }

    [Fact]
    public async Task WhenRefreshTokenAsync_ThenReturnsError()
    {
        var result =
            await _provider.RefreshTokenAsync(_caller.Object, "arefreshtoken", CancellationToken.None);

        result.Should().BeError(ErrorCode.NotAuthenticated);
    }
}
#endif