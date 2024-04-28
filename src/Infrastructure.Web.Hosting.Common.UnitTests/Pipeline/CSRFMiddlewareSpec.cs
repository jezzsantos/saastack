using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using Application.Interfaces;
using Application.Interfaces.Services;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Hosting.Common.Pipeline;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.Pipeline;

[Trait("Category", "Unit")]
public class CSRFMiddlewareSpec
{
    private readonly Mock<CSRFMiddleware.ICSRFService> _csrfService;
    private readonly Mock<IHostSettings> _hostSettings;
    private readonly CSRFMiddleware _middleware;
    private readonly Mock<RequestDelegate> _next;
    private readonly ServiceProvider _serviceProvider;
    private readonly Mock<ICallerContextFactory> _callerContextFactory;

    public CSRFMiddlewareSpec()
    {
        var recorder = new Mock<IRecorder>();
        _hostSettings = new Mock<IHostSettings>();
        _hostSettings.Setup(s => s.GetWebsiteHostCSRFEncryptionSecret())
            .Returns("anexcryptionsecret");
        _hostSettings.Setup(s => s.GetWebsiteHostCSRFSigningSecret())
            .Returns("asigningsecret");
        _hostSettings.Setup(s => s.GetWebsiteHostBaseUrl())
            .Returns("https://localhost");
        _next = new Mock<RequestDelegate>();
        _csrfService = new Mock<CSRFMiddleware.ICSRFService>();
        var serviceCollection = new ServiceCollection();
        serviceCollection.AddSingleton<ILoggerFactory>(new LoggerFactory());
        _serviceProvider = serviceCollection.BuildServiceProvider();
        _callerContextFactory = new Mock<ICallerContextFactory>();
        _callerContextFactory.Setup(c => c.Create())
            .Returns(Mock.Of<ICallerContext>(cc => cc.CallerId == "auserid"));

        _middleware = new CSRFMiddleware(_next.Object, recorder.Object, _hostSettings.Object, _csrfService.Object);
    }

    [Fact]
    public async Task WhenInvokeAsyncAndIsIgnoredMethod_ThenContinuesPipeline()
    {
        var context = new DefaultHttpContext
        {
            Request = { Method = HttpMethods.Get }
        };

        await _middleware.InvokeAsync(context, _callerContextFactory.Object);

        _next.Verify(n => n.Invoke(context));
        _csrfService.Verify(
            cs => cs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                It.IsAny<Optional<string>>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAsyncAndMissingHostName_ThenRespondsWithAProblem()
    {
        var context = SetupContext();
        _hostSettings.Setup(s => s.GetWebsiteHostBaseUrl()).Returns("notauri");

        await _middleware.InvokeAsync(context, _callerContextFactory.Object);

        context.Response.Should().BeAProblem(HttpStatusCode.InternalServerError,
            Resources.CSRFMiddleware_InvalidHostName.Format("notauri"));
        _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAsyncAndMissingCookie_ThenRespondsWithAProblem()
    {
        var context = SetupContext();

        await _middleware.InvokeAsync(context, _callerContextFactory.Object);

        context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
            Resources.CSRFMiddleware_MissingCSRFCookieValue);
        _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAsyncAndMissingHeader_ThenRespondsWithAProblem()
    {
        var context = SetupContext();
        context.Request.Cookies = SetupCookies(new Dictionary<string, string>
            { { CSRFConstants.Cookies.AntiCSRF, "ananticsrfcookie" } });

        await _middleware.InvokeAsync(context, _callerContextFactory.Object);

        context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
            Resources.CSRFMiddleware_MissingCSRFHeaderValue);
        _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAsyncAndAuthTokenIsInvalid_ThenRespondsWithAProblem()
    {
        var context = SetupContext();
        context.Request.Cookies = SetupCookies(new Dictionary<string, string>
        {
            { CSRFConstants.Cookies.AntiCSRF, "ananticsrfcookie" },
            { AuthenticationConstants.Cookies.Token, "notavalidtoken" }
        });
        context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));

        await _middleware.InvokeAsync(context, _callerContextFactory.Object);

        context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
            Resources.CSRFMiddleware_InvalidToken);
        _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAsyncAndTokenNotContainUserIdClaim_ThenRespondsWithAProblem()
    {
        var tokenWithoutUserClaim = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: new Claim[] { }
        ));
        var context = SetupContext();
        context.Request.Cookies = SetupCookies(new Dictionary<string, string>
        {
            { CSRFConstants.Cookies.AntiCSRF, "ananticsrfcookie" },
            { AuthenticationConstants.Cookies.Token, tokenWithoutUserClaim }
        });
        context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));

        await _middleware.InvokeAsync(context, _callerContextFactory.Object);

        context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
            Resources.CSRFMiddleware_InvalidToken);
        _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAsyncAndTokensNotVerifiedForNoUser_ThenRespondsWithAProblem()
    {
        var context = SetupContext();
        context.Request.Cookies = SetupCookies(new Dictionary<string, string>
        {
            { CSRFConstants.Cookies.AntiCSRF, "ananticsrfcookie" },
            { AuthenticationConstants.Cookies.Token, string.Empty }
        });
        context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
        _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                It.IsAny<Optional<string>>()))
            .Returns(false);

        await _middleware.InvokeAsync(context, _callerContextFactory.Object);

        context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
            Resources.CSRFMiddleware_InvalidSignature.Format(nameof(Optional.None)));
        _csrfService.Setup(crs => crs.VerifyTokens("ananticsrfheader", "ananticsrfcookie", Optional<string>.None))
            .Returns(false);
        _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAsyncAndTokensNotVerifiedForUser_ThenRespondsWithAProblem()
    {
        var tokenForUser = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: new Claim[]
            {
                new(AuthenticationConstants.Claims.ForId, "auserid")
            }
        ));
        var context = SetupContext();
        context.Request.Cookies = SetupCookies(new Dictionary<string, string>
        {
            { CSRFConstants.Cookies.AntiCSRF, "ananticsrfcookie" },
            { AuthenticationConstants.Cookies.Token, tokenForUser }
        });
        context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
        _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                It.IsAny<Optional<string>>()))
            .Returns(false);

        await _middleware.InvokeAsync(context, _callerContextFactory.Object);

        context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
            Resources.CSRFMiddleware_InvalidSignature.Format("auserid"));
        _csrfService.Setup(crs => crs.VerifyTokens("ananticsrfheader", "ananticsrfcookie", "auserid"))
            .Returns(false);
        _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAsyncAndTokensIsVerifiedButNoOriginAndNoReferer_ThenRespondsWithAProblem()
    {
        var tokenForUser = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: new Claim[]
            {
                new(AuthenticationConstants.Claims.ForId, "auserid")
            }
        ));
        var context = SetupContext();
        context.Request.Cookies = SetupCookies(new Dictionary<string, string>
        {
            { CSRFConstants.Cookies.AntiCSRF, "ananticsrfcookie" },
            { AuthenticationConstants.Cookies.Token, tokenForUser }
        });
        context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
        context.Request.Headers.Append(HttpHeaders.Origin, new StringValues(string.Empty));
        context.Request.Headers.Append(HttpHeaders.Referer, new StringValues(string.Empty));
        _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                It.IsAny<Optional<string>>()))
            .Returns(true);

        await _middleware.InvokeAsync(context, _callerContextFactory.Object);

        context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
            Resources.CSRFMiddleware_MissingOriginAndReferer);
        _csrfService.Setup(crs => crs.VerifyTokens("ananticsrfheader", "ananticsrfcookie", "auserid"))
            .Returns(false);
        _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAsyncAndTokensIsVerifiedButOriginNotHost_ThenRespondsWithAProblem()
    {
        var tokenForUser = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: new Claim[]
            {
                new(AuthenticationConstants.Claims.ForId, "auserid")
            }
        ));
        var context = SetupContext();
        context.Request.Cookies = SetupCookies(new Dictionary<string, string>
        {
            { CSRFConstants.Cookies.AntiCSRF, "ananticsrfcookie" },
            { AuthenticationConstants.Cookies.Token, tokenForUser }
        });
        context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
        context.Request.Headers.Append(HttpHeaders.Origin, new StringValues("anotherhostname"));
        context.Request.Headers.Append(HttpHeaders.Referer, new StringValues(string.Empty));
        _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                It.IsAny<Optional<string>>()))
            .Returns(true);

        await _middleware.InvokeAsync(context, _callerContextFactory.Object);

        context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
            Resources.CSRFMiddleware_OriginMismatched);
        _csrfService.Setup(crs => crs.VerifyTokens("ananticsrfheader", "ananticsrfcookie", "auserid"))
            .Returns(false);
        _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAsyncAndTokensIsVerifiedButRefererNotHost_ThenRespondsWithAProblem()
    {
        var tokenForUser = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: new Claim[]
            {
                new(AuthenticationConstants.Claims.ForId, "auserid")
            }
        ));
        var context = SetupContext();
        context.Request.Cookies = SetupCookies(new Dictionary<string, string>
        {
            { CSRFConstants.Cookies.AntiCSRF, "ananticsrfcookie" },
            { AuthenticationConstants.Cookies.Token, tokenForUser }
        });
        context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
        context.Request.Headers.Append(HttpHeaders.Origin, new StringValues(string.Empty));
        context.Request.Headers.Append(HttpHeaders.Referer, new StringValues("anotherhostname"));
        _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                It.IsAny<Optional<string>>()))
            .Returns(true);

        await _middleware.InvokeAsync(context, _callerContextFactory.Object);

        context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
            Resources.CSRFMiddleware_RefererMismatched);
        _csrfService.Setup(crs => crs.VerifyTokens("ananticsrfheader", "ananticsrfcookie", "auserid"))
            .Returns(false);
        _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAsyncAndTokensIsVerifiedAndOriginIsHostForUser_ThenContinuesPipeline()
    {
        var tokenForUser = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: new Claim[]
            {
                new(AuthenticationConstants.Claims.ForId, "auserid")
            }
        ));
        var context = SetupContext();
        context.Request.Cookies = SetupCookies(new Dictionary<string, string>
        {
            { CSRFConstants.Cookies.AntiCSRF, "ananticsrfcookie" },
            { AuthenticationConstants.Cookies.Token, tokenForUser }
        });
        context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
        context.Request.Headers.Append(HttpHeaders.Origin, new StringValues("https://localhost"));
        context.Request.Headers.Append(HttpHeaders.Referer, new StringValues(string.Empty));
        _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                It.IsAny<Optional<string>>()))
            .Returns(true);

        await _middleware.InvokeAsync(context, _callerContextFactory.Object);

        context.Response.Should().NotBeAProblem();
        _csrfService.Setup(crs => crs.VerifyTokens("ananticsrfheader", "ananticsrfcookie", "auserid"))
            .Returns(false);
        _next.Verify(n => n.Invoke(context));
    }

    [Fact]
    public async Task WhenInvokeAsyncAndTokensIsVerifiedAndRefererIsHostForUser_ThenContinuesPipeline()
    {
        var tokenForUser = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
            claims: new Claim[]
            {
                new(AuthenticationConstants.Claims.ForId, "auserid")
            }
        ));
        var context = SetupContext();
        context.Request.Cookies = SetupCookies(new Dictionary<string, string>
        {
            { CSRFConstants.Cookies.AntiCSRF, "ananticsrfcookie" },
            { AuthenticationConstants.Cookies.Token, tokenForUser }
        });
        context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
        context.Request.Headers.Append(HttpHeaders.Origin, new StringValues(string.Empty));
        context.Request.Headers.Append(HttpHeaders.Referer, new StringValues("https://localhost"));
        _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                It.IsAny<Optional<string>>()))
            .Returns(true);

        await _middleware.InvokeAsync(context, _callerContextFactory.Object);

        context.Response.Should().NotBeAProblem();
        _csrfService.Setup(crs => crs.VerifyTokens("ananticsrfheader", "ananticsrfcookie", "auserid"))
            .Returns(false);
        _next.Verify(n => n.Invoke(context));
    }

    [Fact]
    public async Task WhenInvokeAsyncAndTokensIsVerifiedAndOriginIsHostForNoUser_ThenContinuesPipeline()
    {
        var context = SetupContext();
        context.Request.Cookies = SetupCookies(new Dictionary<string, string>
        {
            { CSRFConstants.Cookies.AntiCSRF, "ananticsrfcookie" }
        });
        context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
        context.Request.Headers.Append(HttpHeaders.Origin, new StringValues("https://localhost"));
        context.Request.Headers.Append(HttpHeaders.Referer, new StringValues(string.Empty));
        _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                It.IsAny<Optional<string>>()))
            .Returns(true);

        await _middleware.InvokeAsync(context, _callerContextFactory.Object);

        context.Response.Should().NotBeAProblem();
        _csrfService.Setup(crs => crs.VerifyTokens("ananticsrfheader", "ananticsrfcookie", Optional<string>.None))
            .Returns(false);
        _next.Verify(n => n.Invoke(context));
    }

    [Fact]
    public async Task WhenInvokeAsyncAndTokensIsVerifiedAndRefererIsHostForNoUser_ThenContinuesPipeline()
    {
        var context = SetupContext();
        context.Request.Cookies = SetupCookies(new Dictionary<string, string>
        {
            { CSRFConstants.Cookies.AntiCSRF, "ananticsrfcookie" }
        });
        context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
        context.Request.Headers.Append(HttpHeaders.Origin, new StringValues(string.Empty));
        context.Request.Headers.Append(HttpHeaders.Referer, new StringValues("https://localhost"));
        _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                It.IsAny<Optional<string>>()))
            .Returns(true);

        await _middleware.InvokeAsync(context, _callerContextFactory.Object);

        context.Response.Should().NotBeAProblem();
        _csrfService.Setup(crs => crs.VerifyTokens("ananticsrfheader", "ananticsrfcookie", Optional<string>.None))
            .Returns(false);
        _next.Verify(n => n.Invoke(context));
    }

    private DefaultHttpContext SetupContext()
    {
        var context = new DefaultHttpContext
        {
            Request = { Method = HttpMethods.Post },
            RequestServices = _serviceProvider,
            Response =
            {
                StatusCode = 200,
                Body = new MemoryStream()
            }
        };
        return context;
    }

    private static IRequestCookieCollection SetupCookies(Dictionary<string, string> values)
    {
        var cookies = new Mock<IRequestCookieCollection>();
        foreach (var value in values)
        {
            cookies.Setup(c => c.TryGetValue(value.Key, out It.Ref<string?>.IsAny))
                .Returns((string _, ref string? val) =>
                {
                    val = value.Value;
                    return value.Value.HasValue();
                });
        }

        return cookies.Object;
    }
}