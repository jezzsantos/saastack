using System.IdentityModel.Tokens.Jwt;
using Application.Resources.Shared;
using Common.Configuration;
using Domain.Services.Shared.DomainServices;
using FluentAssertions;
using IdentityInfrastructure.ApplicationServices;
using Infrastructure.Interfaces;
using Moq;
using UnitTesting.Common.Validation;
using Xunit;

namespace IdentityInfrastructure.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class JWTTokensServiceSpec
{
    private readonly JWTTokensService _service;
    private readonly Mock<ITokensService> _tokensService;

    public JWTTokensServiceSpec()
    {
        var settings = new Mock<IConfigurationSettings>();
        settings.Setup(s => s.Platform.GetString(JWTTokensService.BaseUrlSettingName, null))
            .Returns("https://localhost");
        settings.Setup(s => s.Platform.GetString(JWTTokensService.SecretSettingName, null))
            .Returns("asecretsigningkey");
        settings.Setup(s => s.Platform.GetNumber(It.IsAny<string>(), It.IsAny<double>()))
            .Returns((string _, double defaultValue) => defaultValue);
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateTokenForJwtRefresh())
            .Returns("arefreshtoken");

        _service = new JWTTokensService(settings.Object, _tokensService.Object);
    }

    [Fact]
    public async Task WhenIssueTokensAsync_ThenReturnsTokens()
    {
        var user = new EndUser
        {
            Access = EndUserAccess.Enabled,
            Status = EndUserStatus.Unregistered,
            Id = "anid",
            Roles = new List<string> { "arole" },
            FeatureLevels = new List<string> { "afeaturelevel" }
        };

        var result = await _service.IssueTokensAsync(user);

        result.Value.AccessToken.Should().NotBeEmpty();
        result.Value.RefreshToken.Should().Be("arefreshtoken");
        result.Value.ExpiresOn.Should().BeNear(DateTime.UtcNow.Add(AuthenticationConstants.DefaultAccessTokenExpiry));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);

        token.Issuer.Should().Be("https://localhost");
        token.Audiences.Should().ContainSingle(aud => aud == "https://localhost");
        token.ValidTo.Should().BeNear(DateTime.UtcNow.Add(AuthenticationConstants.DefaultAccessTokenExpiry),
            TimeSpan.FromMinutes(1));
        token.Claims.Count().Should().Be(6);
        token.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.ClaimForId && claim.Value == "anid");
        token.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.ClaimForRole && claim.Value == "arole");
        token.Claims.Should()
            .Contain(claim =>
                claim.Type == AuthenticationConstants.ClaimForFeatureLevel && claim.Value == "afeaturelevel");
        _tokensService.Verify(ts => ts.CreateTokenForJwtRefresh());
    }
}