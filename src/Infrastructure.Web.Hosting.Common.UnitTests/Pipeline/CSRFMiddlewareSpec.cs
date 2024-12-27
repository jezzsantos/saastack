using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using Application.Interfaces;
using Application.Interfaces.Services;
using Common;
using Common.Extensions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Hosting.Common.Pipeline;
using Infrastructure.Web.Interfaces;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.Pipeline;

[UsedImplicitly]
public class CSRFMiddlewareSpec
{
    private static DefaultHttpContext SetupContext(IServiceProvider serviceProvider)
    {
        var context = new DefaultHttpContext
        {
            Request = { Method = HttpMethods.Post },
            RequestServices = serviceProvider,
            Response =
            {
                StatusCode = 200,
                Body = new MemoryStream()
            }
        };
        return context;
    }

    private static IRequestCookieCollection SetupCookies(bool antiCsrf, string? authToken = null,
        string? lastUserId = null)
    {
        var dictionary = new Dictionary<string, string>();
        if (antiCsrf)
        {
            dictionary.Add(CSRFConstants.Cookies.AntiCSRF,
                new CSRFMiddleware.CSRFCookie(lastUserId, "asignature").ToCookieValue());
        }

        if (authToken.HasValue())
        {
            dictionary.Add(AuthenticationConstants.Cookies.Token, authToken);
        }

        return SetupCookies(dictionary);
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

    [Trait("Category", "Unit")]
    public class GivenAnyUser
    {
        private readonly Mock<ICallerContextFactory> _callerContextFactory;
        private readonly Mock<CSRFMiddleware.ICSRFService> _csrfService;
        private readonly Mock<IHostSettings> _hostSettings;
        private readonly CSRFMiddleware _middleware;
        private readonly Mock<RequestDelegate> _next;
        private readonly ServiceProvider _serviceProvider;

        public GivenAnyUser()
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
            var context = SetupContext(_serviceProvider);
            _hostSettings.Setup(s => s.GetWebsiteHostBaseUrl()).Returns("notauri");

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.InternalServerError,
                Resources.CSRFMiddleware_InvalidHostName.Format("notauri"));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAsyncAndMissingCookie_ThenRespondsWithAProblem()
        {
            var context = SetupContext(_serviceProvider);

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
                Resources.CSRFMiddleware_MissingCSRFCookieValue);
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAsyncAndMissingHeader_ThenRespondsWithAProblem()
        {
            var context = SetupContext(_serviceProvider);
            context.Request.Cookies = SetupCookies(true);

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
                Resources.CSRFMiddleware_MissingCSRFHeaderValue);
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAsyncAndAuthTokenIsInvalid_ThenRespondsWithAProblem()
        {
            var context = SetupContext(_serviceProvider);
            context.Request.Cookies = SetupCookies(true, "notavalidtoken");
            context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
                Resources.CSRFMiddleware_InvalidAuthCookie);
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAsyncAndTokenNotContainUserIdClaim_ThenRespondsWithAProblem()
        {
            var tokenWithoutUserClaim = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                claims: []
            ));
            var context = SetupContext(_serviceProvider);
            context.Request.Cookies = SetupCookies(true, tokenWithoutUserClaim);
            context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.Forbidden, Resources.CSRFMiddleware_InvalidAuthCookie);
            _csrfService.Verify(
                crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>()), Times.Never);
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAsyncAndTokensIsVerifiedButNoOriginAndNoReferer_ThenRespondsWithAProblem()
        {
            var tokenForUser = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                claims:
                [
                    new Claim(AuthenticationConstants.Claims.ForId, "auserid")
                ]
            ));
            var context = SetupContext(_serviceProvider);
            context.Request.Cookies = SetupCookies(true, tokenForUser);
            context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
            context.Request.Headers.Append(HttpConstants.Headers.Origin, new StringValues(string.Empty));
            context.Request.Headers.Append(HttpConstants.Headers.Referer, new StringValues(string.Empty));
            _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>()))
                .Returns(true);

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
                Resources.CSRFMiddleware_MissingOriginAndReferer);
            _csrfService.Verify(crs => crs.VerifyTokens("ananticsrfheader", "asignature", "auserid"));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAsyncAndTokensIsVerifiedButOriginNotHost_ThenRespondsWithAProblem()
        {
            var tokenForUser = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                claims:
                [
                    new Claim(AuthenticationConstants.Claims.ForId, "auserid")
                ]
            ));
            var context = SetupContext(_serviceProvider);
            context.Request.Cookies = SetupCookies(true, tokenForUser);
            context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
            context.Request.Headers.Append(HttpConstants.Headers.Origin, new StringValues("anotherhostname"));
            context.Request.Headers.Append(HttpConstants.Headers.Referer, new StringValues(string.Empty));
            _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>()))
                .Returns(true);

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
                Resources.CSRFMiddleware_OriginMismatched);
            _csrfService.Verify(crs => crs.VerifyTokens("ananticsrfheader", "asignature", "auserid"));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAsyncAndTokensIsVerifiedButRefererNotHost_ThenRespondsWithAProblem()
        {
            var tokenForUser = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                claims:
                [
                    new Claim(AuthenticationConstants.Claims.ForId, "auserid")
                ]
            ));
            var context = SetupContext(_serviceProvider);
            context.Request.Cookies = SetupCookies(true, tokenForUser);
            context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
            context.Request.Headers.Append(HttpConstants.Headers.Origin, new StringValues(string.Empty));
            context.Request.Headers.Append(HttpConstants.Headers.Referer, new StringValues("anotherhostname"));
            _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>()))
                .Returns(true);

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
                Resources.CSRFMiddleware_RefererMismatched);
            _csrfService.Verify(crs => crs.VerifyTokens("ananticsrfheader", "asignature", "auserid"));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
        }
    }

    [Trait("Category", "Unit")]
    public class GivenUnauthenticatedUser
    {
        private readonly Mock<ICallerContextFactory> _callerContextFactory;
        private readonly Mock<CSRFMiddleware.ICSRFService> _csrfService;
        private readonly CSRFMiddleware _middleware;
        private readonly Mock<RequestDelegate> _next;
        private readonly ServiceProvider _serviceProvider;

        public GivenUnauthenticatedUser()
        {
            var recorder = new Mock<IRecorder>();
            var hostSettings = new Mock<IHostSettings>();
            hostSettings.Setup(s => s.GetWebsiteHostCSRFEncryptionSecret())
                .Returns("anexcryptionsecret");
            hostSettings.Setup(s => s.GetWebsiteHostCSRFSigningSecret())
                .Returns("asigningsecret");
            hostSettings.Setup(s => s.GetWebsiteHostBaseUrl())
                .Returns("https://localhost");
            _next = new Mock<RequestDelegate>();
            _csrfService = new Mock<CSRFMiddleware.ICSRFService>();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILoggerFactory>(new LoggerFactory());
            _serviceProvider = serviceCollection.BuildServiceProvider();
            _callerContextFactory = new Mock<ICallerContextFactory>();
            _callerContextFactory.Setup(c => c.Create())
                .Returns(Mock.Of<ICallerContext>(cc => cc.CallerId == "auserid"));

            _middleware = new CSRFMiddleware(_next.Object, recorder.Object, hostSettings.Object, _csrfService.Object);
        }

        [Fact]
        public async Task WhenInvokeAsyncAndTokensNotVerified_ThenRespondsWithAProblem()
        {
            var context = SetupContext(_serviceProvider);
            context.Request.Cookies = SetupCookies(true, string.Empty);
            context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
            _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>()))
                .Returns(false);

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
                Resources.CSRFMiddleware_InvalidSignature);
            _csrfService.Verify(crs => crs.VerifyTokens("ananticsrfheader", "asignature", Optional<string>.None));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAsyncAndTokensIsVerifiedAndOriginIsHost_ThenContinuesPipeline()
        {
            var context = SetupContext(_serviceProvider);
            context.Request.Cookies = SetupCookies(true);
            context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
            context.Request.Headers.Append(HttpConstants.Headers.Origin, new StringValues("https://localhost"));
            context.Request.Headers.Append(HttpConstants.Headers.Referer, new StringValues(string.Empty));
            _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>()))
                .Returns(true);

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().NotBeAProblem();
            _csrfService.Verify(crs => crs.VerifyTokens("ananticsrfheader", "asignature", Optional<string>.None));
            _next.Verify(n => n.Invoke(context));
        }

        [Fact]
        public async Task WhenInvokeAsyncAndTokensIsVerifiedAndRefererIsHost_ThenContinuesPipeline()
        {
            var context = SetupContext(_serviceProvider);
            context.Request.Cookies = SetupCookies(true);
            context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
            context.Request.Headers.Append(HttpConstants.Headers.Origin, new StringValues(string.Empty));
            context.Request.Headers.Append(HttpConstants.Headers.Referer, new StringValues("https://localhost"));
            _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>()))
                .Returns(true);

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().NotBeAProblem();
            _csrfService.Verify(crs => crs.VerifyTokens("ananticsrfheader", "asignature", Optional<string>.None));
            _next.Verify(n => n.Invoke(context));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAuthenticatedUser
    {
        private readonly Mock<ICallerContextFactory> _callerContextFactory;
        private readonly Mock<CSRFMiddleware.ICSRFService> _csrfService;
        private readonly CSRFMiddleware _middleware;
        private readonly Mock<RequestDelegate> _next;
        private readonly ServiceProvider _serviceProvider;

        public GivenAuthenticatedUser()
        {
            var recorder = new Mock<IRecorder>();
            var hostSettings = new Mock<IHostSettings>();
            hostSettings.Setup(s => s.GetWebsiteHostCSRFEncryptionSecret())
                .Returns("anexcryptionsecret");
            hostSettings.Setup(s => s.GetWebsiteHostCSRFSigningSecret())
                .Returns("asigningsecret");
            hostSettings.Setup(s => s.GetWebsiteHostBaseUrl())
                .Returns("https://localhost");
            _next = new Mock<RequestDelegate>();
            _csrfService = new Mock<CSRFMiddleware.ICSRFService>();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILoggerFactory>(new LoggerFactory());
            _serviceProvider = serviceCollection.BuildServiceProvider();
            _callerContextFactory = new Mock<ICallerContextFactory>();
            _callerContextFactory.Setup(c => c.Create())
                .Returns(Mock.Of<ICallerContext>(cc => cc.CallerId == "auserid"));

            _middleware = new CSRFMiddleware(_next.Object, recorder.Object, hostSettings.Object, _csrfService.Object);
        }

        [Fact]
        public async Task WhenInvokeAsyncAndTokensNotVerified_ThenRespondsWithAProblem()
        {
            var tokenForUser = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                claims:
                [
                    new Claim(AuthenticationConstants.Claims.ForId, "auserid")
                ]
            ));
            var context = SetupContext(_serviceProvider);
            context.Request.Cookies = SetupCookies(true, tokenForUser);
            context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
            _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>()))
                .Returns(false);

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
                Resources.CSRFMiddleware_InvalidSignature);
            _csrfService.Verify(crs => crs.VerifyTokens("ananticsrfheader", "asignature", "auserid"));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAsyncAndTokensIsVerifiedAndOriginIsHost_ThenContinuesPipeline()
        {
            var tokenForUser = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                claims:
                [
                    new Claim(AuthenticationConstants.Claims.ForId, "auserid")
                ]
            ));
            var context = SetupContext(_serviceProvider);
            context.Request.Cookies = SetupCookies(true, tokenForUser);
            context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
            context.Request.Headers.Append(HttpConstants.Headers.Origin, new StringValues("https://localhost"));
            context.Request.Headers.Append(HttpConstants.Headers.Referer, new StringValues(string.Empty));
            _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>()))
                .Returns(true);

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().NotBeAProblem();
            _csrfService.Verify(crs => crs.VerifyTokens("ananticsrfheader", "asignature", "auserid"));
            _next.Verify(n => n.Invoke(context));
        }

        [Fact]
        public async Task WhenInvokeAsyncAndTokensIsVerifiedAndRefererIsHost_ThenContinuesPipeline()
        {
            var tokenForUser = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(
                claims:
                [
                    new Claim(AuthenticationConstants.Claims.ForId, "auserid")
                ]
            ));
            var context = SetupContext(_serviceProvider);
            context.Request.Cookies = SetupCookies(true, tokenForUser);
            context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
            context.Request.Headers.Append(HttpConstants.Headers.Origin, new StringValues(string.Empty));
            context.Request.Headers.Append(HttpConstants.Headers.Referer, new StringValues("https://localhost"));
            _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>()))
                .Returns(true);

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().NotBeAProblem();
            _csrfService.Verify(crs => crs.VerifyTokens("ananticsrfheader", "asignature", "auserid"));
            _next.Verify(n => n.Invoke(context));
        }
    }

    [Trait("Category", "Unit")]
    public class GivenAnExpiredAuthenticatedUser
    {
        private readonly Mock<ICallerContextFactory> _callerContextFactory;
        private readonly Mock<CSRFMiddleware.ICSRFService> _csrfService;
        private readonly CSRFMiddleware _middleware;
        private readonly Mock<RequestDelegate> _next;
        private readonly ServiceProvider _serviceProvider;

        public GivenAnExpiredAuthenticatedUser()
        {
            var recorder = new Mock<IRecorder>();
            var hostSettings = new Mock<IHostSettings>();
            hostSettings.Setup(s => s.GetWebsiteHostCSRFEncryptionSecret())
                .Returns("anexcryptionsecret");
            hostSettings.Setup(s => s.GetWebsiteHostCSRFSigningSecret())
                .Returns("asigningsecret");
            hostSettings.Setup(s => s.GetWebsiteHostBaseUrl())
                .Returns("https://localhost");
            _next = new Mock<RequestDelegate>();
            _csrfService = new Mock<CSRFMiddleware.ICSRFService>();
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddSingleton<ILoggerFactory>(new LoggerFactory());
            _serviceProvider = serviceCollection.BuildServiceProvider();
            _callerContextFactory = new Mock<ICallerContextFactory>();
            _callerContextFactory.Setup(c => c.Create())
                .Returns(Mock.Of<ICallerContext>(cc => cc.CallerId == "auserid"));

            _middleware = new CSRFMiddleware(_next.Object, recorder.Object, hostSettings.Object, _csrfService.Object);
        }

        [Fact]
        public async Task WhenInvokeAsyncAndTokensNotVerified_ThenRespondsWithAProblem()
        {
            var context = SetupContext(_serviceProvider);
            context.Request.Cookies = SetupCookies(true, null, "alastuserid");
            context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
            _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>()))
                .Returns(false);

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().BeAProblem(HttpStatusCode.Forbidden,
                Resources.CSRFMiddleware_InvalidSignature);
            _csrfService.Verify(crs => crs.VerifyTokens("ananticsrfheader", "asignature", "alastuserid"));
            _next.Verify(n => n.Invoke(It.IsAny<HttpContext>()), Times.Never);
        }

        [Fact]
        public async Task WhenInvokeAsyncAndTokensIsVerifiedAndOriginIsHost_ThenContinuesPipeline()
        {
            var context = SetupContext(_serviceProvider);
            context.Request.Cookies = SetupCookies(true, null, "alastuserid");
            context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
            context.Request.Headers.Append(HttpConstants.Headers.Origin, new StringValues("https://localhost"));
            context.Request.Headers.Append(HttpConstants.Headers.Referer, new StringValues(string.Empty));
            _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>()))
                .Returns(true);

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().NotBeAProblem();
            _csrfService.Verify(crs => crs.VerifyTokens("ananticsrfheader", "asignature", "alastuserid"));
            _next.Verify(n => n.Invoke(context));
        }

        [Fact]
        public async Task WhenInvokeAsyncAndTokensIsVerifiedAndRefererIsHost_ThenContinuesPipeline()
        {
            var context = SetupContext(_serviceProvider);
            context.Request.Cookies = SetupCookies(true, null, "alastuserid");
            context.Request.Headers.Append(CSRFConstants.Headers.AntiCSRF, new StringValues("ananticsrfheader"));
            context.Request.Headers.Append(HttpConstants.Headers.Origin, new StringValues(string.Empty));
            context.Request.Headers.Append(HttpConstants.Headers.Referer, new StringValues("https://localhost"));
            _csrfService.Setup(crs => crs.VerifyTokens(It.IsAny<Optional<string>>(), It.IsAny<Optional<string>>(),
                    It.IsAny<Optional<string>>()))
                .Returns(true);

            await _middleware.InvokeAsync(context, _callerContextFactory.Object);

            context.Response.Should().NotBeAProblem();
            _csrfService.Verify(crs => crs.VerifyTokens("ananticsrfheader", "asignature", "alastuserid"));
            _next.Verify(n => n.Invoke(context));
        }
    }
}