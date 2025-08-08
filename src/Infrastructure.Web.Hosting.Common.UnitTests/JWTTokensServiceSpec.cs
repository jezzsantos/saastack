using System.IdentityModel.Tokens.Jwt;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Services.Shared;
using FluentAssertions;
using Infrastructure.Common.Extensions;
using Moq;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests;

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
        settings.Setup(s => s.Platform.GetString(JWTTokensService.SigningPublicKeySettingName, null))
            .Returns(
                "-----BEGIN RSA PUBLIC KEY-----\nMIIBCgKCAQEAoZlYKp93rSsf5ZO1Xzj857bqzASga+L9vfw3qAjEl3NqOOTILvtS\n+4Sw+IUZ0qv4xRTNXZmaLPy4fwLByCZX496y0buJ8ouvevR7etYFNn9NIKJcphV6\njRyHFG6YgUejHzcwdSyhKuc9kPEjuOGhjSs1+94+VJmrYqUjFDgjMOl/GqVQhHww\nQzbWiZD6gJiICXpIUpBo0K65TmwGBgm/Zj5ImZZI0aKmbLY4aod5LiTO8JCqzE9K\nphH/XBb7oXYlURQ8DHLFnfsIwO7VTD6qS6LiBY+9Vv6YFMJ4oinULOJJt9EJgvbC\nzbvSGiY37k6y4Dn2jbmwcDjE2J7z0muNmQIDAQAB\n-----END RSA PUBLIC KEY-----");
        settings.Setup(s => s.Platform.GetString(JWTTokensService.SigningPrivateKeySettingName, null))
            .Returns(
                "-----BEGIN RSA PRIVATE KEY-----\nMIIEowIBAAKCAQEAoZlYKp93rSsf5ZO1Xzj857bqzASga+L9vfw3qAjEl3NqOOTI\nLvtS+4Sw+IUZ0qv4xRTNXZmaLPy4fwLByCZX496y0buJ8ouvevR7etYFNn9NIKJc\nphV6jRyHFG6YgUejHzcwdSyhKuc9kPEjuOGhjSs1+94+VJmrYqUjFDgjMOl/GqVQ\nhHwwQzbWiZD6gJiICXpIUpBo0K65TmwGBgm/Zj5ImZZI0aKmbLY4aod5LiTO8JCq\nzE9KphH/XBb7oXYlURQ8DHLFnfsIwO7VTD6qS6LiBY+9Vv6YFMJ4oinULOJJt9EJ\ngvbCzbvSGiY37k6y4Dn2jbmwcDjE2J7z0muNmQIDAQABAoIBAELk0GtsccT8WgrV\n1zmgxIhC3vUvYRzn7PPNSVjEsGSlQS5l/jv8i4BUkFF//42G5Mboco6xe/Htd44U\nHRV2UeGhGVLamCMQEccLF2Zk2+mQTuQYcdPKhl2NlpktovG5LtxII0YOAHVbHdA3\nEfuBYzel0IX/nLu2lQyToW7IQkEHbrfrCLdjADQSYxagqaotlrwpJOOcrjMLM3dE\nPexY1ckqufxQSxBm01hsfnNQOfE1/+wGEd7fCV8NPmWMJY+evFTRJfHLxE55Sddj\nLayi62Tb7KOeARfyuYWRyVGfEyC3NBcDz60kjB2FAVrZMiNXrbmL7nWGUbdD/wN/\nhw9mDhUCgYEAxFzgLc7sS17P3hoiZUFAL0GXbG8dCBUMkrNYvlSgo78W4XmnnX4X\n85/xP6dCmW+RQZJNp3+FlhaQR+0miewA6NVH2/kwLHPBjMG2BgY87aSzEAjeXtMM\nWpzsX/25R1UytlVKjsnCvKbCKfVdru0bv0PCh8sLOSlI5XZGR16vV08CgYEA0q2Z\nMZiMPv/pxrllk/JcT5eVF9cX0+W3SJTxxQWMOqHzGtmXPMqQaOuDxHVpkdowFPgQ\nz/AuE/Yye/xsZk/VqGcGOP/oqLa+6Xsoy6bRkFdDJfwaClpG3uc4ey7K2XL8p29l\nwcLV7mjeUh0KUQEH6Vx0CRajo940hQ3nhqaMkpcCgYA44hLLEltfRhr/YyC9plZa\nmiysa8/ELJzUzoGRuWBDrzKIpL5KoGF94MJ5RxHC8w/oJ+K49/cR0H2BaJC2eZiV\n2lsTvS8YYXwbM25wdlQmH4UDyx3n6El24miTMiP/Jw4mxbRwgsAX+FLc5sh5yRQ1\npwJuZgJdT7lfR5D6UdKHfwKBgQC5AYdovSxTWoohX8dqz0bvAg8EW3dqNezoyRsy\nx/dnubXxWyjrUnrUGBWjXPPzB+Z3U4v3/lOIZgfZR0at5eebNbWKMnhOSASIpgWi\nKQEYvviRj7wSYUqhDe1UhzfNEqP6KOHz8DPLY73v396iWcRn0i93l7DmAwid2yL4\n5KLHSwKBgE5Ur9cLXOzqfqYF1ouh481rHHeCTeu95TleRbNJgl6uvNCn9CczwtFf\nggcMUdcGJO7YO/Ia3QNQp2RuhWY0+BgwQo4CERKsXjXW1wMWOI1kcA25HqDRAsdv\np94k3eqVhStinrXbkMp3VrpXnDlgOvXgozmZm058JxiXQwpAoqW2\n-----END RSA PRIVATE KEY-----");
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
        var (privateKey, publicKey) = JWTTokensService.GenerateSigningKey();

        privateKey.Should().NotBeNullOrEmpty();
        publicKey.Should().NotBeNullOrEmpty();
#endif
    }

    [Fact]
    public async Task WhenIssueTokensAsyncWithoutProfileAndNoScopes_ThenReturnsTokens()
    {
        var now = DateTime.UtcNow.ToNearestSecond();
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
            .BeNear(now.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry));

        var token = new JwtSecurityTokenHandler().ReadJwtToken(result.Value.AccessToken);

        token.Issuer.Should().Be("https://localhost");
        token.Audiences.Should().OnlyContain(aud => aud == "https://localhost");
        token.ValidTo.Should().BeNear(now.Add(AuthenticationConstants.Tokens.DefaultAccessTokenExpiry),
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
    public async Task WhenIssueTokensAsyncWithProfileButNoClientIdInAdditionalData_ThenReturnsError()
    {
        var user = new EndUserWithMemberships
        {
            Id = "anid"
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
            Classification = UserProfileClassification.Person
        };
        var additionalData = new Dictionary<string, object>();

        var result = await _service.IssueTokensAsync(user, profile, [
            OAuth2Constants.Scopes.Profile,
            OAuth2Constants.Scopes.Email
        ], additionalData);

        result.Should().BeError(ErrorCode.PreconditionViolation, Resources.JWTTokensService_ClientIdRequired);
        _tokensService.Verify(ts => ts.CreateJWTRefreshToken());
    }

    [Fact]
    public async Task WhenIssueTokensAsyncWithProfileAndScopes_ThenReturnsTokensWithIdToken()
    {
        var now = DateTime.UtcNow.ToNearestSecond();
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
            Locale = "alocale",
            AvatarUrl = "anavatarurl",
            Classification = UserProfileClassification.Person
        };
        var additionalData = new Dictionary<string, object>
        {
            { AuthenticationConstants.Claims.ForNonce, "anonce" },
            { AuthenticationConstants.Claims.ForAtHash, "anathash" },
            { AuthenticationConstants.Claims.ForCHash, "achash" },
            { AuthenticationConstants.Claims.ForAuthTime, now },
            { AuthenticationConstants.Claims.ForClientId, "aclientid" }
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
        accessToken.Claims.Count().Should().Be(12);
        accessToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForId && claim.Value == "anid");
        accessToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForIssuedAt);
        accessToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForScope
                              && claim.Value == $"{OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}");
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
        idToken.Audiences.Should().OnlyContain(aud => aud == "aclientid");
        idToken.ValidTo.Should().BeNear(now.Add(AuthenticationConstants.Tokens.DefaultIdTokenExpiry),
            TimeSpan.FromMinutes(1));
        idToken.Claims.Count().Should().Be(20);
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForId && claim.Value == "anid");
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForIssuedAt);
        idToken.Claims.Should()
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForScope
                              && claim.Value == $"{OAuth2Constants.Scopes.Profile} {OAuth2Constants.Scopes.Email}");
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
            .Contain(claim => claim.Type == AuthenticationConstants.Claims.ForLocale && claim.Value == "alocale");
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