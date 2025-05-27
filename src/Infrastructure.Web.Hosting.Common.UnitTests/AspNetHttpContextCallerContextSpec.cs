using System.Security.Claims;
using Application.Interfaces.Services;
using Common;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests;

[Trait("Category", "Unit")]
public class AspNetHttpContextCallerContextSpec
{
    private readonly Mock<IHostSettings> _hostSettings;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;
    private readonly Mock<ITenancyContext> _tenancyContext;

    public AspNetHttpContextCallerContextSpec()
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
            new AspNetHttpContextCallerContext(_tenancyContext.Object, _hostSettings.Object,
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
        _httpContextAccessor.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>());

        var result =
            new AspNetHttpContextCallerContext(_tenancyContext.Object, _hostSettings.Object,
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
            new AspNetHttpContextCallerContext(_tenancyContext.Object, _hostSettings.Object,
                _httpContextAccessor.Object);

        result.IsAuthenticated.Should().BeTrue();
    }
}