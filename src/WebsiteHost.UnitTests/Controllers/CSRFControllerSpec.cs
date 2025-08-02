using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Interfaces;
using Common;
using FluentAssertions;
using Infrastructure.Web.Hosting.Common.Pipeline;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Moq;
using WebsiteHost.ApplicationServices;
using WebsiteHost.Controllers;
using WebsiteHost.Models;
using Xunit;

namespace WebsiteHost.UnitTests.Controllers;

[Trait("Category", "Unit")]
public class CSRFControllerSpec
{
    private readonly TestController _controller;
    private readonly Mock<IRequestCookieCollection> _cookies;
    private readonly Mock<CSRFMiddleware.ICSRFService> _csrfService;

    public CSRFControllerSpec()
    {
        var hostEnvironment = new Mock<IHostEnvironment>();
        hostEnvironment.Setup(x => x.ContentRootPath)
            .Returns("apath");
        _csrfService = new Mock<CSRFMiddleware.ICSRFService>();
        var webPackBundler = new Mock<IWebPackBundler>();
        webPackBundler.Setup(x => x.GetBundleName("apath"))
            .Returns("abundle");
        _controller = new TestController(hostEnvironment.Object, _csrfService.Object, webPackBundler.Object);
        _cookies = new Mock<IRequestCookieCollection>();
        _controller.ControllerContext.HttpContext = new DefaultHttpContext
        {
            Request =
            {
                Cookies = _cookies.Object
            }
        };
    }

    [Fact]
    public void WhenAnAction_ThenReturnsJsContext()
    {
        _csrfService.Setup(x => x.CreateTokens(It.IsAny<Optional<string>>()))
            .Returns(CSRFTokenPair.FromTokens("atoken", "asignature"));

        var result = _controller.AnAction();

        result.Should().BeOfType<ViewResult>();
        var view = (ViewResult)result;
        view.Model.Should().BeOfType<IndexSpaPage>();
        var model = view.Model.As<IndexSpaPage>();
#if HOSTEDONAZURE
        model.IsHostedOn.Should().Be("AZURE");
#endif
#if HOSTEDONAWS
        model.IsHostedOn.Should().Be("AWS");
#endif
        model.IsTestingOnly.Should().BeTrue();
        model.JsBundleName.Should().Be("abundle");
        _csrfService.Verify(x => x.CreateTokens(Optional<string>.None));
    }

    [Fact]
    public void WhenAnActionAndNoAuthCookies_ThenSetsCSRFCookieForNoUser()
    {
        _csrfService.Setup(x => x.CreateTokens(It.IsAny<Optional<string>>()))
            .Returns(CSRFTokenPair.FromTokens("atoken", "asignature"));

        var result = _controller.AnAction();

        var model = result.As<ViewResult>().Model.As<IndexSpaPage>();
        model.CSRFFieldName.Should().Be(CSRFConstants.Html.CSRFRequestFieldName);
        model.CSRFHeaderToken.Should().Be("atoken");
        var response = _controller.ControllerContext.HttpContext.Response;
        var csrfCookie = response.Headers[HttpConstants.Headers.SetCookie][0];
        csrfCookie.Should()
            .StartWith($"{CSRFConstants.Cookies.AntiCSRF}=eyJTaWduYXR1cmUiOiJhc2lnbmF0dXJlIn0%3D;");
        csrfCookie.Should().Contain("httponly");
        _csrfService.Verify(x => x.CreateTokens(Optional<string>.None));
    }

    [Fact]
    public void WhenAnActionAndAuthCookie_ThenSetsCSRFCookieForUser()
    {
        _csrfService.Setup(x => x.CreateTokens(It.IsAny<Optional<string>>()))
            .Returns(CSRFTokenPair.FromTokens("atoken", "asignature"));
        var userId = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: [new Claim(AuthenticationConstants.Claims.ForId, "auserid")]
        ));
        _cookies.Setup(coo => coo.TryGetValue(It.IsAny<string>(), out userId))
            .Returns(true);

        var result = _controller.AnAction();

        var model = result.As<ViewResult>().Model.As<IndexSpaPage>();
        model.CSRFFieldName.Should().Be(CSRFConstants.Html.CSRFRequestFieldName);
        model.CSRFHeaderToken.Should().Be("atoken");
        var response = _controller.ControllerContext.HttpContext.Response;
        var csrfCookie = response.Headers[HttpConstants.Headers.SetCookie][0];
        csrfCookie.Should()
            .StartWith(
                $"{CSRFConstants.Cookies.AntiCSRF}=eyJMYXN0VXNlcklkIjoiYXVzZXJpZCIsIlNpZ25hdHVyZSI6ImFzaWduYXR1cmUifQ%3D%3D;");
        csrfCookie.Should().Contain("httponly");
        _csrfService.Verify(x => x.CreateTokens("auserid"));
    }
}

public class TestController : CSRFController
{
    public TestController(IHostEnvironment hostEnvironment, CSRFMiddleware.ICSRFService csrfService,
        IWebPackBundler webPackBundler) : base(
        hostEnvironment, csrfService, webPackBundler)
    {
    }

    public IActionResult AnAction()
    {
        return CSRFResult();
    }
}