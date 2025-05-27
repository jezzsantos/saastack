using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Interfaces.Services;
using Common;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests;

[Trait("Category", "Unit")]
public class AspNetBeffeCallerContextSpec
{
    private readonly Mock<IRequestCookieCollection> _cookies;
    private readonly Mock<IHostSettings> _hostSettings;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessor;

    public AspNetBeffeCallerContextSpec()
    {
        _hostSettings = new Mock<IHostSettings>();
        _hostSettings.Setup(h => h.GetRegion())
            .Returns(DatacenterLocations.AustraliaEast);
        var hostSettings = new Mock<IHostSettings>();
        hostSettings.Setup(h => h.GetRegion())
            .Returns(DatacenterLocations.AustraliaEast);
        _cookies = new Mock<IRequestCookieCollection>();
        _httpContextAccessor = new Mock<IHttpContextAccessor>();
        _httpContextAccessor.Setup(hc => hc.HttpContext!.Items).Returns(new Dictionary<object, object?>());
        _httpContextAccessor.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>());
        _httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Headers).Returns(new HeaderDictionary());
        _httpContextAccessor.Setup(hc => hc.HttpContext!.Features).Returns(new FeatureCollection());
        _httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Cookies).Returns(_cookies.Object);
    }

    [Fact]
    public void WhenConstructed_ThenDetails()
    {
        _httpContextAccessor.Setup(hc => hc.HttpContext!.User.Claims).Returns(new List<Claim>());

        var result =
            new AspNetBeffeCallerContext(_hostSettings.Object, _httpContextAccessor.Object);

        result.IsAuthenticated.Should().BeFalse();
        result.IsServiceAccount.Should().BeFalse();
        result.CallId.Should().NotBeEmpty();
        result.CallerId.Should().Be(CallerConstants.AnonymousUserId);
        result.TenantId.Should().BeNone();
        result.Roles.All.Length.Should().Be(0);
        result.Features.All.Length.Should().Be(0);
        result.HostRegion.Should().Be(DatacenterLocations.AustraliaEast);
    }

    [Fact]
    public void WhenConstructedAndNoAuthCookie_ThenIsNotAuthenticated()
    {
        _httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Cookies).Returns(_cookies.Object);

        var result =
            new AspNetBeffeCallerContext(_hostSettings.Object, _httpContextAccessor.Object);

        result.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void WhenConstructedAndAuthCookieWithAuthorization_ThenIsAuthenticated()
    {
        _cookies.Setup(c => c.TryGetValue(AuthenticationConstants.Cookies.Token, out It.Ref<string>.IsAny!))
            .Returns((string _, out string value) =>
            {
                var token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                    claims:
                    [
                        new Claim(AuthenticationConstants.Claims.ForId, "auserid")
                    ]
                ));
                value = token;
                return true;
            });
        _httpContextAccessor.Setup(hc => hc.HttpContext!.Request.Cookies).Returns(_cookies.Object);

        var result =
            new AspNetBeffeCallerContext(_hostSettings.Object, _httpContextAccessor.Object);

        result.IsAuthenticated.Should().BeTrue();
    }
}