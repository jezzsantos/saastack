using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.Interfaces;
using Application.Interfaces.Services;
using Common;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Hosting.Common.Auth;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Web.Hosting.Common.UnitTests.Auth;

[Trait("Category", "Unit")]
public class PrivateInterHostAuthenticationHandlerSpec
{
    private readonly PrivateInterHostAuthenticationHandler _handler;
    private readonly Mock<IHostSettings> _hostSettings;
    private readonly DefaultHttpContext _httpContext;
    private readonly Mock<IRecorder> _recorder;
    private readonly ServiceCollection _serviceCollection;
    private readonly Mock<IAuthenticationHandlerProvider> _authenticationProvider;

    public PrivateInterHostAuthenticationHandlerSpec()
    {
        var options = new Mock<IOptionsMonitor<PrivateInterHostOptions>>();
        options.Setup(x => x.Get(It.IsAny<string>()))
            .Returns(new PrivateInterHostOptions());
        _serviceCollection = [];
        _recorder = new Mock<IRecorder>();
        _serviceCollection.AddSingleton(_recorder.Object);
        var caller = new Mock<ICallerContext>();
        var callerFactory = new Mock<ICallerContextFactory>();
        callerFactory.Setup(cf => cf.Create()).Returns(caller.Object);
        _serviceCollection.AddSingleton(callerFactory.Object);
        _hostSettings = new Mock<IHostSettings>();
        _hostSettings.Setup(hs => hs.GetPrivateInterHostHmacAuthSecret()).Returns("asecret");
        _serviceCollection.AddSingleton(_hostSettings.Object);
        _authenticationProvider = new Mock<IAuthenticationHandlerProvider>();
        _serviceCollection.AddSingleton(_authenticationProvider.Object);
        _httpContext = new DefaultHttpContext
        {
            Request =
            {
                IsHttps = true
            },
            RequestServices = _serviceCollection.BuildServiceProvider()
        };

        _handler = new PrivateInterHostAuthenticationHandler(options.Object, new LoggerFactory(), UrlEncoder.Default);
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndConnectionNotSecure_ThenReturnsFailure()
    {
        _httpContext.Request.IsHttps = false;
        _handler.InitializeAsync(new AuthenticationScheme(PrivateInterHostAuthenticationHandler.AuthenticationScheme,
            null,
            typeof(PrivateInterHostAuthenticationHandler)), _httpContext).GetAwaiter().GetResult();

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().BeOfType<AuthenticationFailureException>()
            .Which.Message.Should().Be(Resources.AuthenticationHandler_NotHttps);
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndNoSignatureHeader_ThenReturnsNoResult()
    {
        _handler.InitializeAsync(new AuthenticationScheme(PrivateInterHostAuthenticationHandler.AuthenticationScheme,
            null,
            typeof(PrivateInterHostAuthenticationHandler)), _httpContext).GetAwaiter().GetResult();

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().BeNull();
        result.None.Should().BeTrue();
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndNoSecret_ThenReturnsFailure()
    {
        _httpContext.Request.Headers[HttpConstants.Headers.PrivateInterHostSignature] = "asignature";
        _hostSettings.Setup(hs => hs.GetPrivateInterHostHmacAuthSecret()).Returns(string.Empty);
        _httpContext.RequestServices = _serviceCollection.BuildServiceProvider();
        await _handler.InitializeAsync(new AuthenticationScheme(
            PrivateInterHostAuthenticationHandler.AuthenticationScheme, null,
            typeof(PrivateInterHostAuthenticationHandler)), _httpContext);

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        _recorder.Verify(rec => rec.Audit(It.IsAny<ICallContext>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
        _recorder.Verify(rec => rec.TraceError(It.IsAny<ICallContext>(),
            Resources.PrivateInterHostAuthenticationHandler_Misconfigured_NoSecret));
        result.Failure.Should().BeOfType<AuthenticationFailureException>()
            .Which.Message.Should()
            .Be(Resources.AuthenticationHandler_Failed);
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndWrongSignature_ThenReturnsFailure()
    {
        _httpContext.Request.Headers[HttpConstants.Headers.PrivateInterHostSignature] = "asignature";
        await _handler.InitializeAsync(new AuthenticationScheme(
            PrivateInterHostAuthenticationHandler.AuthenticationScheme, null,
            typeof(PrivateInterHostAuthenticationHandler)), _httpContext);

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        _recorder.Verify(rec => rec.Audit(It.IsAny<ICallContext>(),
            AuditingConstants.PrivateInterHostAuthenticationFailed,
            Resources.PrivateInterHostAuthenticationHandler_FailedAuthentication));
        result.Failure.Should().BeOfType<AuthenticationFailureException>()
            .Which.Message.Should()
            .Be(Resources.AuthenticationHandler_Failed);
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndSignatureMatchesButJwtProviderNotRegistered_ThenReturnsFailure()
    {
        var body = new byte[] { 0x01 };
        var signature = new HMACSigner(body, "asecret").Sign();
        _httpContext.Request.Headers[HttpConstants.Headers.PrivateInterHostSignature] = signature;
        _httpContext.Request.Body = new MemoryStream(body);
        await _handler.InitializeAsync(new AuthenticationScheme(
            PrivateInterHostAuthenticationHandler.AuthenticationScheme, null,
            typeof(PrivateInterHostAuthenticationHandler)), _httpContext);
        _authenticationProvider.Setup(ap => ap.GetHandlerAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync((IAuthenticationHandler?)null);
        _serviceCollection.AddSingleton(_authenticationProvider.Object);

        var result = await _handler.AuthenticateAsync();

        _recorder.Verify(rec => rec.Audit(It.IsAny<ICallContext>(), It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
        _recorder.Verify(rec => rec.TraceError(It.IsAny<ICallContext>(),
            Resources.PrivateInterHostAuthenticationHandler_Misconfigured_JwtProvider));
        result.Failure.Should().BeOfType<AuthenticationFailureException>()
            .Which.Message.Should()
            .Be(Resources.AuthenticationHandler_Failed);
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndSignatureMatchesButNoJwtToken_ThenReturnsAuthenticatedForAnonymous()
    {
        var body = new byte[] { 0x01 };
        var signature = new HMACSigner(body, "asecret").Sign();
        _httpContext.Request.Headers[HttpConstants.Headers.PrivateInterHostSignature] = signature;
        _httpContext.Request.Body = new MemoryStream(body);
        await _handler.InitializeAsync(new AuthenticationScheme(
            PrivateInterHostAuthenticationHandler.AuthenticationScheme, null,
            typeof(PrivateInterHostAuthenticationHandler)), _httpContext);
        var jwtProvider = new Mock<IAuthenticationHandler>();
        jwtProvider.Setup(jp => jp.AuthenticateAsync())
            .ReturnsAsync(AuthenticateResult.NoResult());
        _authenticationProvider.Setup(ap => ap.GetHandlerAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync(jwtProvider.Object);
        _serviceCollection.AddSingleton(_authenticationProvider.Object);

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Ticket?.Principal.Claims.Should().Contain(claim =>
            claim.Type == AuthenticationConstants.Claims.ForId
            && claim.Value == CallerConstants.AnonymousUserId);
        _recorder.Verify(rec => rec.Audit(It.IsAny<ICallContext>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task
        WhenHandleAuthenticateAsyncAndSignatureMatchesAndNoTokenAuthenticated_ThenReturnsAuthenticatedForTokenUser()
    {
        var body = new byte[] { 0x01 };
        var signature = new HMACSigner(body, "asecret").Sign();
        _httpContext.Request.Headers[HttpConstants.Headers.PrivateInterHostSignature] = signature;
        _httpContext.Request.Body = new MemoryStream(body);
        await _handler.InitializeAsync(new AuthenticationScheme(
            PrivateInterHostAuthenticationHandler.AuthenticationScheme, null,
            typeof(PrivateInterHostAuthenticationHandler)), _httpContext);
        var jwtProvider = new Mock<IAuthenticationHandler>();
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(), "ascheme");
        jwtProvider.Setup(jp => jp.AuthenticateAsync())
            .ReturnsAsync(AuthenticateResult.Success(ticket));
        _authenticationProvider.Setup(ap => ap.GetHandlerAsync(It.IsAny<HttpContext>(), It.IsAny<string>()))
            .ReturnsAsync(jwtProvider.Object);
        _serviceCollection.AddSingleton(_authenticationProvider.Object);

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Ticket.Should().Be(ticket);
        _recorder.Verify(rec => rec.Audit(It.IsAny<ICallContext>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}