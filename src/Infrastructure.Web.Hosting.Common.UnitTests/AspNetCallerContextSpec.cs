using System.Security.Claims;
using Application.Interfaces;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common;
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

    public AspNetCallerContextSpec()
    {
        _httpContext = new Mock<IHttpContextAccessor>();
        _httpContext.Setup(hc => hc.HttpContext!.Items).Returns(new Dictionary<object, object?>());
        _httpContext.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>());
        _httpContext.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary());
        _httpContext.Setup(hc => hc.HttpContext!.Features).Returns(new FeatureCollection());
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
            new(AuthenticationConstants.ClaimForId, "auserid")
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
    public void WhenConstructedAndContainsRolesClaims_ThenSetsRoles()
    {
        _httpContext.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>
        {
            new(AuthenticationConstants.ClaimForRole, "arole1"),
            new(AuthenticationConstants.ClaimForRole, "arole2"),
            new(AuthenticationConstants.ClaimForRole, "arole3")
        });

        var result = new AspNetCallerContext(_httpContext.Object);

        result.Roles.All.Should().ContainInOrder("arole1", "arole2", "arole3");
    }

    [Fact]
    public void WhenConstructedAndNoFeatureLevelsClaim_ThenSetsEmptyFeatureLevels()
    {
        var result = new AspNetCallerContext(_httpContext.Object);

        result.FeatureLevels.All.Should().BeEmpty();
    }

    [Fact]
    public void WhenConstructedAndContainsFeatureLevelsClaims_ThenSetsFeatureLevels()
    {
        _httpContext.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>
        {
            new(AuthenticationConstants.ClaimForFeatureLevel, "alevel1"),
            new(AuthenticationConstants.ClaimForFeatureLevel, "alevel2"),
            new(AuthenticationConstants.ClaimForFeatureLevel, "alevel3")
        });

        var result = new AspNetCallerContext(_httpContext.Object);

        result.FeatureLevels.All.Should().Contain(x => x.Name == "alevel1");
        result.FeatureLevels.All.Should().Contain(x => x.Name == "alevel2");
        result.FeatureLevels.All.Should().Contain(x => x.Name == "alevel3");
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
            { HttpHeaders.Authorization, "Bearer atoken" }
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
            { HttpHeaders.Authorization, "Bearer atoken" }
        });
        _httpContext.Setup(hc => hc.HttpContext!.Request.Query)
            .Returns(new QueryCollection(new Dictionary<string, StringValues>
            {
                { HttpQueryParams.APIKey, "anapikey" }
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
            { HttpHeaders.Authorization, "Bearer atoken" }
        });
        _httpContext.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>
        {
            new(AuthenticationConstants.ClaimForId, "auserid")
        });

        var result = new AspNetCallerContext(_httpContext.Object);

        result.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public void WhenConstructedAndHasServiceAccountUserClaim_ThenIsServiceAccount()
    {
        _httpContext.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>
        {
            new(AuthenticationConstants.ClaimForId, CallerConstants.ServiceClientAccountUserId)
        });

        var result = new AspNetCallerContext(_httpContext.Object);

        result.IsAuthenticated.Should().BeTrue();
        result.IsServiceAccount.Should().BeTrue();
    }
}