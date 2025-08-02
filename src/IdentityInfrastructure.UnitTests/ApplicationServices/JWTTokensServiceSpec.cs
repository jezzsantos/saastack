using System.IdentityModel.Tokens.Jwt;
using Application.Interfaces;
using Application.Resources.Shared;
using Common.Configuration;
using Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Services.Shared;
using FluentAssertions;
using IdentityInfrastructure.ApplicationServices;
using Infrastructure.Common.Extensions;
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
        settings.Setup(s => s.Platform.GetString(JWTTokensService.SigningSecretSettingName, null))
            .Returns("asecretsigningkeyasecretsigningkeyasecretsigningkeyasecretsigningkey");
        settings.Setup(s => s.Platform.GetNumber(It.IsAny<string>(), It.IsAny<double>()))
            .Returns((string _, double defaultValue) => defaultValue);
        _tokensService = new Mock<ITokensService>();
        _tokensService.Setup(ts => ts.CreateJWTRefreshToken())
            .Returns("arefreshtoken");

        _service = new JWTTokensService(settings.Object, _tokensService.Object);
    }

    [Fact]
    public void WhenGenerateSigningKey_ThenReturnsKey()
    {
#if TESTINGONLY
        var result = JWTTokensService.GenerateSigningKey();

        result.Should().NotBeNullOrEmpty();
#endif
    }

    [Fact]
    public async Task WhenIssueTokensAsyncWithoutProfile_ThenReturnsTokens()
    {
        var user = new EndUserWithMemberships
        {
            Access = EndUserAccess.Enabled,
            Status = EndUserStatus.Unregistered,
            Id = "anid",
            Roles = [PlatformRoles.Standard.Name],
            Features = [PlatformFeatures.Basic.Name],
            Memberships =
            [
                new Membership
                {
                    Id = "amembershipid",
                    UserId = "auserid",
                    OrganizationId = "anorganizationid",
                    Roles = [TenantRoles.Member.Name],
                    Features = [TenantFeatures.Basic.Name]
                }
            ]
        };

        var result = await _service.IssueTokensAsync(user, null, null, null);

        result.Value.AccessToken.Should().NotBeEmpty();
        result.Value.RefreshToken.Should().Be("arefreshtoken");
        result.Value.AccessTokenExpiresOn.Should()
            .BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);

        token.Issuer.Should().Be("https://localhost");
        token.Audiences.Should().OnlyContain(aud => aud == "https://localhost");
        token.ValidTo.Should().BeNear(DateTime.UtcNow.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry),
            TimeSpan.FromMinutes(1));
        token.Claims.Count().Should().Be(9);
        token.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForId && claim.Value == "anid");
        token.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForIssuedAt);
        token.Claims.Should()
            .Contain(
                claim => claim.Type == AuthenticationConstants.Claims.ForRole
                         && claim.Value == $"Platform_{PlatformRoles.Standard.Name}");
        token.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForRole
                              && claim.Value
                              == $"Tenant_{TenantRoles.Member.Name}{ClaimExtensions.TenantIdDelimiter}anorganizationid");
        token.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForFeature
                              && claim.Value == $"Platform_{PlatformFeatures.Basic.Name}");
        token.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForFeature
                              && claim.Value
                              == $"Tenant_{TenantFeatures.Basic.Name}{ClaimExtensions.TenantIdDelimiter}anorganizationid");
        _tokensService.Verify(ts => ts.CreateJWTRefreshToken());
    }

    [Fact]
    public async Task WhenIssueTokensAsyncWithProfile_ThenReturnsTokensWithIdToken()
    {
        var user = new EndUserWithMemberships
        {
            Access = EndUserAccess.Enabled,
            Status = EndUserStatus.Unregistered,
            Id = "anid",
            Roles = [PlatformRoles.Standard.Name],
            Features = [PlatformFeatures.Basic.Name],
            Memberships =
            [
                new Membership
                {
                    Id = "amembershipid",
                    UserId = "auserid",
                    OrganizationId = "anorganizationid",
                    Roles = [TenantRoles.Member.Name],
                    Features = [TenantFeatures.Basic.Name]
                }
            ]
        };
        var profile = new UserProfile
        {
            Id = "aprofileid",
            UserId = "anid",
            Name = new PersonName
            {
                FirstName = "afirstname",
                LastName = "alastname"
            },
            DisplayName = "adisplayname",
            Address = new ProfileAddress
            {
                CountryCode = "acountrycode"
            },
            EmailAddress = "anemailaddress",
            PhoneNumber = "aphonenumber",
            Timezone = "atimezone",
            AvatarUrl = "anavatarurl",
            Classification = UserProfileClassification.Person
        };
        var now = DateTime.UtcNow.ToNearestSecond();
        var additionalData = new Dictionary<string, object>
        {
            { AuthenticationConstants.Claims.ForNonce, "anonce" },
            { AuthenticationConstants.Claims.ForAtHash, "anathash" },
            { AuthenticationConstants.Claims.ForCHash, "achash" },
            { AuthenticationConstants.Claims.ForAuthTime, now }
        };

        var result = await _service.IssueTokensAsync(user, profile, [
            OAuth2Constants.Scopes.Profile,
            OAuth2Constants.Scopes.Email
        ], additionalData);

        result.Value.AccessToken.Should().NotBeEmpty();
        result.Value.RefreshToken.Should().Be("arefreshtoken");
        result.Value.AccessTokenExpiresOn.Should()
            .BeNear(now.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));
        result.Value.IdToken.Should().NotBeEmpty();
        result.Value.IdTokenExpiresOn.Should()
            .BeNear(now.Add(AuthenticationConstants.Tokens.DefaultIdTokenExpiry));

        var accessToken = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);
        accessToken.Issuer.Should().Be("https://localhost");
        accessToken.Audiences.Should().OnlyContain(aud => aud == "https://localhost");
        accessToken.ValidTo.Should().BeNear(
            now.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry),
            TimeSpan.FromMinutes(1));
        accessToken.Claims.Count().Should().Be(11);
        accessToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForId && claim.Value == "anid");
        accessToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForIssuedAt);
        accessToken.Claims.Should()
            .Contain(
                claim => claim.Type == AuthenticationConstants.Claims.ForRole
                         && claim.Value == $"Platform_{PlatformRoles.Standard.Name}");
        accessToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForRole
                              && claim.Value
                              == $"Tenant_{TenantRoles.Member.Name}{ClaimExtensions.TenantIdDelimiter}anorganizationid");
        accessToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForFeature
                              && claim.Value == $"Platform_{PlatformFeatures.Basic.Name}");
        accessToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForFeature
                              && claim.Value
                              == $"Tenant_{TenantFeatures.Basic.Name}{ClaimExtensions.TenantIdDelimiter}anorganizationid");
        accessToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForAtHash && claim.Value == "anathash");
        accessToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForCHash && claim.Value == "achash");
        _tokensService.Verify(ts => ts.CreateJWTRefreshToken());

        var idToken = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.IdToken);
        idToken.Issuer.Should().Be("https://localhost");
        idToken.Audiences.Should().OnlyContain(aud => aud == "https://localhost");
        idToken.ValidTo.Should().BeNear(now.Add(AuthenticationConstants.Tokens.DefaultIdTokenExpiry),
            TimeSpan.FromMinutes(1));
        idToken.Claims.Count().Should().Be(18);
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForId && claim.Value == "anid");
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForIssuedAt);
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForGivenName && claim.Value == "afirstname");
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForFamilyName && claim.Value == "alastname");
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForFullName
                              && claim.Value == "afirstname alastname");
        idToken.Claims.Should()
            .Contain(claim =>
                claim.Type == AuthenticationConstants.Claims.ForNickName && claim.Value == "adisplayname");
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForEmail && claim.Value == "anemailaddress");
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForEmailVerified && claim.Value == "True");
        idToken.Claims.Should()
            .Contain(claim =>
                claim.Type == AuthenticationConstants.Claims.ForPhoneNumber && claim.Value == "aphonenumber");
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForTimezone && claim.Value == "atimezone");
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForPicture && claim.Value == "anavatarurl");
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForNonce && claim.Value == "anonce");
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForAtHash && claim.Value == "anathash");
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForCHash && claim.Value == "achash");
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForAuthTime);
    }
}