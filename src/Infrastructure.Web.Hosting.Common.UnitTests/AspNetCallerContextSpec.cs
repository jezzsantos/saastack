using System.Security.Claims;
using Application.Interfaces;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using FluentAssertions;
using Infrastructure.Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Endpoints;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Hosting.Common.Auth;
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
public class AspNetCallerContextSpec
{
    private readonly Mock<IHttpContextAccessor> _httpContext;
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly Mock<ITenancyContext> _tenancyContext;

    public AspNetCallerContextSpec()
    {
        _tenancyContext = new Mock<ITenancyContext>();
        _tenancyContext.Setup(tc => tc.Current)
            .Returns("atenantid");
        _serviceProvider = new Mock<IServiceProvider>();
        _serviceProvider.Setup(sp => sp.GetService(typeof(ITenancyContext)))
            .Returns(_tenancyContext.Object);
        _httpContext = new Mock<IHttpContextAccessor>();
        _httpContext.Setup(hc => hc.HttpContext!.Items).Returns(new Dictionary<object, object?>());
        _httpContext.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>());
        _httpContext.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary());
        _httpContext.Setup(hc => hc.HttpContext!.Features).Returns(new FeatureCollection());
        _httpContext.Setup(hc => hc.HttpContext!.RequestServices).Returns(_serviceProvider.Object);
    }

    [Fact]
    public void WhenConstructedAndNoTenancyContext_ThenNoTenant()
    {
        _serviceProvider.Setup(sp => sp.GetService(typeof(ITenancyContext)))
            .Returns((ITenancyContext?)null);

        var result = new AspNetCallerContext(_httpContext.Object);

        result.TenantId.Should().BeNull();
    }

    [Fact]
    public void WhenConstructedAndNoTenantInTenancyContext_ThenNoTenant()
    {
        _tenancyContext.Setup(tc => tc.Current)
            .Returns((string?)null);

        var result = new AspNetCallerContext(_httpContext.Object);

        result.TenantId.Should().BeNull();
    }

    [Fact]
    public void WhenConstructed_ThenTenantId()
    {
        var result = new AspNetCallerContext(_httpContext.Object);

        result.TenantId.Should().Be("atenantid");
    }

    [Fact]
    public void WhenConstructedAndHttpRequestHasNoCorrelationId_ThenFabricatesCallId()
    {
        var result = new AspNetCallerContext(_httpContext.Object);

        result.CallId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void WhenConstructedAndHttpRequestHasCorrelationId_ThenSetsCallId()
    {
        _httpContext.Setup(hc => hc.HttpContext!.Items).Returns(new Dictionary<object, object?>
        {
            { RequestCorrelationFilter.CorrelationIdItemName, "acorrelationid" }
        });

        var result = new AspNetCallerContext(_httpContext.Object);

        result.CallId.Should().Be("acorrelationid");
    }

    [Fact]
    public void WhenConstructedAndNoUserClaim_ThenSetsAnonymousCallerId()
    {
        var result = new AspNetCallerContext(_httpContext.Object);

        result.CallerId.Should().Be(CallerConstants.AnonymousUserId);
    }

    [Fact]
    public void WhenConstructedAndContainsUserClaim_ThenSetsCallerId()
    {
        _httpContext.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>
        {
            new(AuthenticationConstants.Claims.ForId, "auserid")
        });

        var result = new AspNetCallerContext(_httpContext.Object);

        result.CallerId.Should().Be("auserid");
    }

    [Fact]
    public void WhenConstructedAndNoRolesClaim_ThenSetsEmptyRoles()
    {
        var result = new AspNetCallerContext(_httpContext.Object);

        result.Roles.All.Should().BeEmpty();
    }

    [Fact]
    public void WhenConstructedAndNoFeaturesClaim_ThenSetsEmptyFeatures()
    {
        var result = new AspNetCallerContext(_httpContext.Object);

        result.Features.All.Should().BeEmpty();
    }

    [Fact]
    public void WhenConstructedAndUnknownRolesClaim_ThenSetsEmptyRoles()
    {
        _httpContext.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>
        {
            new(AuthenticationConstants.Claims.ForRole, "arole1"),
            new(AuthenticationConstants.Claims.ForRole, "arole2"),
            new(AuthenticationConstants.Claims.ForRole, "arole3")
        });

        var result = new AspNetCallerContext(_httpContext.Object);

        result.Roles.All.Should().BeEmpty();
    }

    [Fact]
    public void WhenConstructedAndContainsRolesClaims_ThenSetsRoles()
    {
        _httpContext.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>
        {
            new(AuthenticationConstants.Claims.ForRole,
                ClaimExtensions.ToPlatformClaimValue(PlatformRoles.Standard)),
            new(AuthenticationConstants.Claims.ForRole,
                ClaimExtensions.ToTenantClaimValue(TenantRoles.Member,
                    "atenantid"))
        });

        var result = new AspNetCallerContext(_httpContext.Object);

        result.Roles.All.Should().ContainInOrder(PlatformRoles.Standard, TenantRoles.Member);
        result.Roles.Platform.Should().OnlyContain(rol => rol == PlatformRoles.Standard);
        result.Roles.Tenant.Should().OnlyContain(rol => rol == TenantRoles.Member);
    }

    [Fact]
    public void WhenConstructedAndUnknownFeaturesClaim_ThenSetsEmptyFeatures()
    {
        _httpContext.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>
        {
            new(AuthenticationConstants.Claims.ForFeature, "afeature1"),
            new(AuthenticationConstants.Claims.ForFeature, "afeature2"),
            new(AuthenticationConstants.Claims.ForFeature, "afeature3")
        });

        var result = new AspNetCallerContext(_httpContext.Object);

        result.Features.All.Should().BeEmpty();
    }

    [Fact]
    public void WhenConstructedAndContainsFeaturesClaims_ThenSetsFeatures()
    {
        _httpContext.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>
        {
            new(AuthenticationConstants.Claims.ForFeature,
                ClaimExtensions.ToPlatformClaimValue(PlatformFeatures.Basic)),
            new(AuthenticationConstants.Claims.ForFeature,
                ClaimExtensions.ToTenantClaimValue(TenantFeatures.Basic,
                    "atenantid"))
        });

        var result = new AspNetCallerContext(_httpContext.Object);

        result.Features.All.Should().ContainInOrder(PlatformFeatures.Basic);
        result.Features.Platform.Should().OnlyContain(feat => feat == PlatformFeatures.Basic);
        result.Features.Tenant.Should().OnlyContain(feat => feat == TenantFeatures.Basic);
    }

    [Fact]
    public void WhenConstructedAndHttpRequestHasNoBearerToken_ThenResetsAuthorization()
    {
        var result = new AspNetCallerContext(_httpContext.Object);

        result.Authorization.Should().BeNone();
    }

    [Fact]
    public void WhenConstructedAndNoAuthenticationFeatureUsed_ThenResetsAuthorization()
    {
        var result = new AspNetCallerContext(_httpContext.Object);

        result.Authorization.Should().BeNone();
    }

    [Fact]
    public void WhenConstructedAndNoAuthenticationTicketForThisRequest_ThenResetsAuthorization()
    {
        var authFeature = new Mock<IAuthenticateResultFeature>();
        var features = new FeatureCollection();
        features.Set(authFeature.Object);
        _httpContext.Setup(hc => hc.HttpContext!.Features).Returns(features);

        var result = new AspNetCallerContext(_httpContext.Object);

        result.Authorization.Should().BeNone();
    }

    [Fact]
    public void WhenConstructedAndUnknownAuthenticationScheme_ThenResetsAuthorization()
    {
        var authFeature = new Mock<IAuthenticateResultFeature>();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), "anunknownscheme");
        authFeature.Setup(af => af.AuthenticateResult)
            .Returns(AuthenticateResult.Success(ticket));
        var features = new FeatureCollection();
        features.Set(authFeature.Object);
        _httpContext.Setup(hc => hc.HttpContext!.Features).Returns(features);

        var result = new AspNetCallerContext(_httpContext.Object);

        result.Authorization.Should().BeNone();
    }

    [Fact]
    public void WhenConstructedAndHMACAuthScheme_ThenResetsAuthorization()
    {
        var authFeature = new Mock<IAuthenticateResultFeature>();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), HMACAuthenticationHandler.AuthenticationScheme);
        authFeature.Setup(af => af.AuthenticateResult)
            .Returns(AuthenticateResult.Success(ticket));
        var features = new FeatureCollection();
        features.Set(authFeature.Object);
        _httpContext.Setup(hc => hc.HttpContext!.Features).Returns(features);

        var result = new AspNetCallerContext(_httpContext.Object);

        result.Authorization.Should().BeNone();
    }

    [Fact]
    public void WhenConstructedAndTokenAuthScheme_ThenSetsAuthorization()
    {
        var authFeature = new Mock<IAuthenticateResultFeature>();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), JwtBearerDefaults.AuthenticationScheme);
        authFeature.Setup(af => af.AuthenticateResult)
            .Returns(AuthenticateResult.Success(ticket));
        var features = new FeatureCollection();
        features.Set(authFeature.Object);
        _httpContext.Setup(hc => hc.HttpContext!.Features).Returns(features);
        _httpContext.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, "Bearer atoken" }
        });

        var result = new AspNetCallerContext(_httpContext.Object);

        result.Authorization.Should().BeSome(auth => auth is
            { Method: ICallerContext.AuthorizationMethod.Token, Value: "atoken" });
    }

    [Fact]
    public void WhenConstructedAndAPIKeyAuthScheme_ThenSetsAuthorization()
    {
        var authFeature = new Mock<IAuthenticateResultFeature>();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), APIKeyAuthenticationHandler.AuthenticationScheme);
        authFeature.Setup(af => af.AuthenticateResult)
            .Returns(AuthenticateResult.Success(ticket));
        var features = new FeatureCollection();
        features.Set(authFeature.Object);
        _httpContext.Setup(hc => hc.HttpContext!.Features).Returns(features);
        _httpContext.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, "Bearer atoken" }
        });
        _httpContext.Setup(hc => hc.HttpContext!.Request.Query)
            .Returns(new QueryCollection(new Dictionary<string, StringValues>
            {
                { HttpConstants.QueryParams.APIKey, "anapikey" }
            }));

        var result = new AspNetCallerContext(_httpContext.Object);

        result.Authorization.Should().BeSome(auth => auth is
            { Method: ICallerContext.AuthorizationMethod.APIKey, Value: "anapikey" });
    }

    [Fact]
    public void WhenConstructedAndNoClaims_ThenNotAuthenticated()
    {
        var result = new AspNetCallerContext(_httpContext.Object);

        result.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void WhenConstructedAndHasUserClaimAndHasBearerToken_ThenSetsAuthenticated()
    {
        var authFeature = new Mock<IAuthenticateResultFeature>();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), JwtBearerDefaults.AuthenticationScheme);
        authFeature.Setup(af => af.AuthenticateResult)
            .Returns(AuthenticateResult.Success(ticket));
        var features = new FeatureCollection();
        features.Set(authFeature.Object);
        _httpContext.Setup(hc => hc.HttpContext!.Features).Returns(features);
        _httpContext.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary
        {
            { HttpConstants.Headers.Authorization, "Bearer atoken" }
        });
        _httpContext.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>
        {
            new(AuthenticationConstants.Claims.ForId, "auserid")
        });

        var result = new AspNetCallerContext(_httpContext.Object);

        result.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void WhenConstructedAndHasServiceAccountUserClaim_ThenIsServiceAccount()
    {
        _httpContext.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>
        {
            new(AuthenticationConstants.Claims.ForId, CallerConstants.ServiceClientAccountUserId)
        });

        var result = new AspNetCallerContext(_httpContext.Object);

        result.IsAuthenticated.Should().BeTrue();
        result.IsServiceAccount.Should().BeTrue();
    }
}