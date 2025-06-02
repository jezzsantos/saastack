using System.Security.Claims;
using Application.Interfaces;
using Application.Interfaces.Services;
using Common;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using FluentAssertions;
using Infrastructure.Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Endpoints;
using Infrastructure.Web.Hosting.Common.Auth;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests;

[Trait("Category", "Unit")]
public class AspNetClaimsBasedCallerContextSpec
{
    private readonly Mock<IHostSettings> _hostSettings;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly Mock<ITenancyContext> _tenancyContext;

    public AspNetClaimsBasedCallerContextSpec()
    {
        _hostSettings = new Mock<IHostSettings>();
        _hostSettings.Setup(h => h.GetRegion())
            .Returns(DatacenterLocations.AustraliaEast);
        _tenancyContext = new Mock<ITenancyContext>();
        _tenancyContext.Setup(tc => tc.Current)
            .Returns("atenantid");
        var hostSettings = new Mock<IHostSettings>();
        hostSettings.Setup(h => h.GetRegion())
            .Returns(DatacenterLocations.AustraliaEast);
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _httpContextAccessor.Setup(hc => hc.HttpContext!.Items).Returns(new Dictionary<object, object?>());
        _httpContextAccessor.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>());
        _httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary());
        _httpContextAccessor.Setup(hc => hc.HttpContext!.Features).Returns(new FeatureCollection());
    }

    [Fact]
    public void WhenConstructed_ThenDetails()
    {
        _httpContextAccessor.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>());

        var result =
            new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
                _httpContextAccessor.Object);

        result.IsAuthenticated.Should().BeFalse();
        result.IsServiceAccount.Should().BeFalse();
        result.CallId.Should().NotBeEmpty();
        result.CallerId.Should().Be(CallerConstants.AnonymousUserId);
        result.TenantId.Should().BeSome("atenantid");
        result.Roles.All.Length.Should().Be(0);
        result.Features.All.Length.Should().Be(0);
        result.HostRegion.Should().Be(DatacenterLocations.AustraliaEast);
    }

    [Fact]
    public void WhenConstructedAndNoClaims_ThenIsNotAuthenticated()
    {
        _httpContextAccessor.Setup(hc => hc.HttpContext!.User.Claims)
            .Returns(new List<Claim>());

        var result =
            new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
                _httpContextAccessor.Object);

        result.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void WhenConstructedAndUserClaimWithAuthorization_ThenIsAuthenticated()
    {
        var authFeature = new Mock<IAuthenticateResultFeature>();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), JwtBearerDefaults.AuthenticationScheme);
        authFeature.Setup(af => af.AuthenticateResult)
            .Returns(AuthenticateResult.Success(ticket));
        var features = new FeatureCollection();
        features.Set(authFeature.Object);
        _httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, "Bearer atoken" }
        });
        _httpContextAccessor.Setup(hc => hc.HttpContext!.Features).Returns(features);
        _httpContextAccessor.Setup(hc => hc.HttpContext!.User.Claims)
            .Returns(new List<Claim> { new(AuthenticationConstants.Claims.ForId, "auserid") });

        var result =
            new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
                _httpContextAccessor.Object);

        result.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void WhenConstructedAndNoTenant_ThenNoTenant()
    {
        _tenancyContext.Setup(tc => tc.Current)
            .Returns(Optional<string>.None);

        var result = new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
            _httpContextAccessor.Object);

        result.TenantId.Should().BeNone();
        result.CallId.Should().NotBeEmpty();
    }

    [Fact]
    public void WhenConstructedAndTenantId_ThenTenantId()
    {
        var result = new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
            _httpContextAccessor.Object);

        result.TenantId.Should().Be("atenantid");
        result.CallId.Should().NotBeEmpty();
    }

    [Fact]
    public void WhenConstructedAndNoUserClaim_ThenSetsAnonymousCallerId()
    {
        var result = new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
            _httpContextAccessor.Object);

        result.CallerId.Should().Be(CallerConstants.AnonymousUserId);
    }

    [Fact]
    public void WhenConstructedAndContainsUserClaim_ThenSetsCallerId()
    {
        _httpContextAccessor.Setup(hc => hc.HttpContext!.User.Claims)
            .Returns([new Claim(AuthenticationConstants.Claims.ForId, "auserid")]);

        var result = new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
            _httpContextAccessor.Object);

        result.CallerId.Should().Be("auserid");
    }

    [Fact]
    public void WhenConstructedAndNoRolesClaim_ThenSetsEmptyRoles()
    {
        var result = new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
            _httpContextAccessor.Object);

        result.Roles.All.Should().BeEmpty();
    }

    [Fact]
    public void WhenConstructedAndNoFeaturesClaim_ThenSetsEmptyFeatures()
    {
        var result = new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
            _httpContextAccessor.Object);

        result.Features.All.Should().BeEmpty();
    }

    [Fact]
    public void WhenConstructedAndUnknownRolesClaim_ThenSetsEmptyRoles()
    {
        _httpContextAccessor.Setup(hc => hc.HttpContext!.User.Claims)
            .Returns([
                new Claim(AuthenticationConstants.Claims.ForRole, "arole1"),
                new Claim(AuthenticationConstants.Claims.ForRole, "arole2"),
                new Claim(AuthenticationConstants.Claims.ForRole, "arole3")
            ]);

        var result = new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
            _httpContextAccessor.Object);

        result.Roles.All.Should().BeEmpty();
    }

    [Fact]
    public void WhenConstructedAndContainsRolesClaims_ThenSetsRoles()
    {
        _httpContextAccessor.Setup(hc => hc.HttpContext!.User.Claims)
            .Returns([
                new Claim(AuthenticationConstants.Claims.ForRole,
                    ClaimExtensions.ToPlatformClaimValue(PlatformRoles.Standard)),
                new Claim(AuthenticationConstants.Claims.ForRole,
                    ClaimExtensions.ToTenantClaimValue(TenantRoles.Member,
                        "atenantid"))
            ]);

        var result = new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
            _httpContextAccessor.Object);

        result.Roles.All.Should().ContainInOrder(PlatformRoles.Standard, TenantRoles.Member);
        result.Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
        result.Roles.Tenant.Should().OnlyContain(rol => rol == TenantRoles.Member);
    }

    [Fact]
    public void WhenConstructedAndUnknownFeaturesClaim_ThenSetsEmptyFeatures()
    {
        _httpContextAccessor.Setup(hc => hc.HttpContext!.User.Claims)
            .Returns([
                new Claim(AuthenticationConstants.Claims.ForFeature, "afeature1"),
                new Claim(AuthenticationConstants.Claims.ForFeature, "afeature2"),
                new Claim(AuthenticationConstants.Claims.ForFeature, "afeature3")
            ]);

        var result = new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
            _httpContextAccessor.Object);

        result.Features.All.Should().BeEmpty();
    }

    [Fact]
    public void WhenConstructedAndContainsFeaturesClaims_ThenSetsFeatures()
    {
        _httpContextAccessor.Setup(hc => hc.HttpContext!.User.Claims)
            .Returns([
                new Claim(AuthenticationConstants.Claims.ForFeature,
                    ClaimExtensions.ToPlatformClaimValue(PlatformFeatures.Basic)),
                new Claim(AuthenticationConstants.Claims.ForFeature,
                    ClaimExtensions.ToTenantClaimValue(TenantFeatures.Basic,
                        "atenantid"))
            ]);

        var result = new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
            _httpContextAccessor.Object);

        result.Features.All.Should().ContainInOrder(PlatformFeatures.Basic);
        result.Features.Platform.Should().OnlyContain(feat => feat == PlatformFeatures.Basic);
        result.Features.Tenant.Should().OnlyContain(feat => feat == TenantFeatures.Basic);
    }

    [Fact]
    public void WhenConstructedAndHttpRequestHasNoBearerToken_ThenResetsAuthorization()
    {
        var result = new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
            _httpContextAccessor.Object);

        result.Authorization.Should().BeNone();
    }

    [Fact]
    public void WhenConstructedAndIsServiceAccountIdentity_ThenIsServiceAccount()
    {
        _httpContextAccessor.Setup(hc => hc.HttpContext!.User.Claims)
            .Returns([new Claim(AuthenticationConstants.Claims.ForId, CallerConstants.ServiceClientAccountUserId)]);

        var result = new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
            _httpContextAccessor.Object);

        result.IsAuthenticated.Should().BeTrue();
        result.IsServiceAccount.Should().BeTrue();
    }

    [Fact]
    public void WhenConstructedAndHasNoAuthorization_ThenIsNotAuthenticated()
    {
        var result = new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
            _httpContextAccessor.Object);

        result.IsAuthenticated.Should().BeFalse();
        result.Authorization.Should().BeNone();
    }

    [Fact]
    public void WhenConstructedAndHasAuthorizationAndNotAnonymous_ThenIsAuthenticated()
    {
        _httpContextAccessor.Setup(hc => hc.HttpContext!.User.Claims)
            .Returns([new Claim(AuthenticationConstants.Claims.ForId, "auserid")]);

        var authFeature = new Mock<IAuthenticateResultFeature>();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), JwtBearerDefaults.AuthenticationScheme);
        authFeature.Setup(af => af.AuthenticateResult)
            .Returns(AuthenticateResult.Success(ticket));
        var features = new FeatureCollection();
        features.Set(authFeature.Object);
        _httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, "Bearer atoken" }
        });
        _httpContextAccessor.Setup(hc => hc.HttpContext!.Features).Returns(features);

        var result = new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
            _httpContextAccessor.Object);

        result.IsAuthenticated.Should().BeTrue();
        result.Authorization.Should().BeSome(auth =>
            auth.Method == ICallerContext.AuthorizationMethod.Token && auth.Value == "atoken".ToOptional());
    }

    [Fact]
    public void WhenConstructedAndHasAuthorizationAndIsAnonymous_ThenIsNotAuthenticated()
    {
        _httpContextAccessor.Setup(hc => hc.HttpContext!.User.Claims)
            .Returns([]);

        var result = new AspNetClaimsBasedCallerContext(_tenancyContext.Object, _hostSettings.Object,
            _httpContextAccessor.Object);

        result.IsAuthenticated.Should().BeFalse();
        result.Authorization.Should().BeNone();
    }

    [Fact]
    public void WhenGetCorrelationIdAndHasNoCorrelationId_ThenFabricatesCallId()
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(hc => hc.HttpContext!.Items)
            .Returns(new Dictionary<object, object?>());

        var result = AspNetClaimsBasedCallerContext.GetCorrelationId(httpContextAccessor.Object);

        result.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WhenGetCorrelationIdAndHasCorrelationId_ThenSetsCallId()
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(hc => hc.HttpContext!.Items)
            .Returns(new Dictionary<object, object?>
                { { RequestCorrelationFilter.CorrelationIdItemName, "acorrelationid" } });

        var result = AspNetClaimsBasedCallerContext.GetCorrelationId(httpContextAccessor.Object);

        result.Should().Be("acorrelationid");
    }

    [Fact]
    public void WhenGetAuthorizationAndNoAuthenticationFeatureUsed_ThenResetsAuthorization()
    {
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary());
        httpContextAccessor.Setup(hc => hc.HttpContext!.Features).Returns(new FeatureCollection());

        var result = AspNetClaimsBasedCallerContext.GetAuthorization(httpContextAccessor.Object);

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetAuthorizationAndNoAuthenticationTicketForThisRequest_ThenResetsAuthorization()
    {
        var authFeature = new Mock<IAuthenticateResultFeature>();
        var features = new FeatureCollection();
        features.Set(authFeature.Object);
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary());
        httpContextAccessor.Setup(hc => hc.HttpContext!.Features).Returns(features);

        var result = AspNetClaimsBasedCallerContext.GetAuthorization(httpContextAccessor.Object);

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetAuthorizationAndUnknownAuthenticationScheme_ThenResetsAuthorization()
    {
        var authFeature = new Mock<IAuthenticateResultFeature>();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), "anunknownscheme");
        authFeature.Setup(af => af.AuthenticateResult)
            .Returns(AuthenticateResult.Success(ticket));
        var features = new FeatureCollection();
        features.Set(authFeature.Object);
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary());
        httpContextAccessor.Setup(hc => hc.HttpContext!.Features).Returns(features);

        var result = AspNetClaimsBasedCallerContext.GetAuthorization(httpContextAccessor.Object);

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetAuthorizationAndHMACAuthScheme_ThenResetsAuthorization()
    {
        var authFeature = new Mock<IAuthenticateResultFeature>();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), HMACAuthenticationHandler.AuthenticationScheme);
        authFeature.Setup(af => af.AuthenticateResult)
            .Returns(AuthenticateResult.Success(ticket));
        var features = new FeatureCollection();
        features.Set(authFeature.Object);
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary());
        httpContextAccessor.Setup(hc => hc.HttpContext!.Features).Returns(features);

        var result = AspNetClaimsBasedCallerContext.GetAuthorization(httpContextAccessor.Object);

        result.Should().BeSome(auth =>
            auth.Method == ICallerContext.AuthorizationMethod.HMAC && auth.Value == Optional<string>.None);
    }

    [Fact]
    public void WhenGetAuthorizationAndPrivateInterHostAuthSchemeButNoJwtToken_ThenSetsAuthorization()
    {
        var authFeature = new Mock<IAuthenticateResultFeature>();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(),
            PrivateInterHostAuthenticationHandler.AuthenticationScheme);
        authFeature.Setup(af => af.AuthenticateResult)
            .Returns(AuthenticateResult.Success(ticket));
        var features = new FeatureCollection();
        features.Set(authFeature.Object);
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary());
        httpContextAccessor.Setup(hc => hc.HttpContext!.Features).Returns(features);

        var result = AspNetClaimsBasedCallerContext.GetAuthorization(httpContextAccessor.Object);

        result.Should().BeSome(auth =>
            auth.Method == ICallerContext.AuthorizationMethod.PrivateInterHost && auth.Value == Optional<string>.None);
    }

    [Fact]
    public void WhenGetAuthorizationAndPrivateInterHostAuthSchemeAndJwtToken_ThenSetsAuthorization()
    {
        var authFeature = new Mock<IAuthenticateResultFeature>();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(),
            PrivateInterHostAuthenticationHandler.AuthenticationScheme);
        authFeature.Setup(af => af.AuthenticateResult)
            .Returns(AuthenticateResult.Success(ticket));
        var features = new FeatureCollection();
        features.Set(authFeature.Object);
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, "Bearer atoken" }
        });
        httpContextAccessor.Setup(hc => hc.HttpContext!.Features).Returns(features);

        var result = AspNetClaimsBasedCallerContext.GetAuthorization(httpContextAccessor.Object);

        result.Should().BeSome(auth =>
            auth.Method == ICallerContext.AuthorizationMethod.PrivateInterHost && auth.Value == "atoken".ToOptional());
    }

    [Fact]
    public void WhenGetAuthorizationAndTokenAuthScheme_ThenSetsAuthorization()
    {
        var authFeature = new Mock<IAuthenticateResultFeature>();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), JwtBearerDefaults.AuthenticationScheme);
        authFeature.Setup(af => af.AuthenticateResult)
            .Returns(AuthenticateResult.Success(ticket));
        var features = new FeatureCollection();
        features.Set(authFeature.Object);
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, "Bearer atoken" }
        });
        httpContextAccessor.Setup(hc => hc.HttpContext!.Features).Returns(features);

        var result = AspNetClaimsBasedCallerContext.GetAuthorization(httpContextAccessor.Object);

        result.Should().BeSome(auth =>
            auth.Method == ICallerContext.AuthorizationMethod.Token && auth.Value == "atoken".ToOptional());
    }

    [Fact]
    public void WhenGetAuthorizationAndAPIKeyAuthScheme_ThenSetsAuthorization()
    {
        var authFeature = new Mock<IAuthenticateResultFeature>();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), APIKeyAuthenticationHandler.AuthenticationScheme);
        authFeature.Setup(af => af.AuthenticateResult)
            .Returns(AuthenticateResult.Success(ticket));
        var features = new FeatureCollection();
        features.Set(authFeature.Object);
        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, "Bearer atoken" }
        });
        httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Query)
            .Returns(new QueryCollection(new Dictionary<string, StringValues>
            {
                { HttpConstants.QueryParams.APIKey, "anapikey" }
            }));
        httpContextAccessor.Setup(hc => hc.HttpContext!.Features).Returns(features);

        var result = AspNetClaimsBasedCallerContext.GetAuthorization(httpContextAccessor.Object);

        result.Should().BeSome(auth =>
            auth.Method == ICallerContext.AuthorizationMethod.APIKey && auth.Value == "anapikey".ToOptional());
    }
}