using System.Security.Claims;
using System.Text.Encodings.Web;
using Application.Interfaces;
using Application.Interfaces.Services;
using Common;
using Common.Extensions;
using Domain.Common.Authorization;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Hosting.Common.Auth;
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
public class HMACAuthenticationHandlerSpec
{
    private readonly HMACAuthenticationHandler _handler;
    private readonly Mock<IHostSettings> _hostSettings;
    private readonly DefaultHttpContext _httpContext;
    private readonly Mock<IRecorder> _recorder;
    private readonly ServiceCollection _serviceCollection;

    public HMACAuthenticationHandlerSpec()
    {
        var clock = new Mock<ISystemClock>();
        var options = new Mock<IOptionsMonitor<HMACOptions>>();
        options.Setup(x => x.Get(It.IsAny<string>()))
            .Returns(new HMACOptions());
        _serviceCollection = new ServiceCollection();
        _recorder = new Mock<IRecorder>();
        _serviceCollection.AddSingleton(_recorder.Object);
        var caller = new Mock<ICallerContext>();
        _serviceCollection.AddSingleton(caller.Object);
        _hostSettings = new Mock<IHostSettings>();
        _hostSettings.Setup(hs => hs.GetAncillaryApiHostHmacAuthSecret()).Returns("asecret");
        _serviceCollection.AddSingleton(_hostSettings.Object);
        _httpContext = new DefaultHttpContext
        {
            Request =
            {
                IsHttps = true
            },
            RequestServices = _serviceCollection.BuildServiceProvider()
        };

        _handler = new HMACAuthenticationHandler(options.Object, new LoggerFactory(), UrlEncoder.Default, clock.Object);
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndConnectionNotSecure_ThenReturnsFailure()
    {
        _httpContext.Request.IsHttps = false;
        _handler.InitializeAsync(new AuthenticationScheme(HMACAuthenticationHandler.AuthenticationScheme, null,
            typeof(HMACAuthenticationHandler)), _httpContext).GetAwaiter().GetResult();

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().BeOfType<Exception>()
            .Which.Message.Should().Be(Resources.HMACAuthenticationHandler_NotHttps);
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndNoSignatureHeader_ThenReturnsFailure()
    {
        _handler.InitializeAsync(new AuthenticationScheme(HMACAuthenticationHandler.AuthenticationScheme, null,
            typeof(HMACAuthenticationHandler)), _httpContext).GetAwaiter().GetResult();

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().BeOfType<Exception>()
            .Which.Message.Should()
            .Be(Resources.HMACAuthenticationHandler_MissingHeader.Format(HttpHeaders.HmacSignature));
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndNoSecret_ThenReturnsAuthenticated()
    {
        _httpContext.Request.Headers.Add(HttpHeaders.HmacSignature, "asignature");
        _hostSettings.Setup(hs => hs.GetAncillaryApiHostHmacAuthSecret()).Returns(string.Empty);
        _httpContext.RequestServices = _serviceCollection.BuildServiceProvider();
        await _handler.InitializeAsync(new AuthenticationScheme(HMACAuthenticationHandler.AuthenticationScheme, null,
            typeof(HMACAuthenticationHandler)), _httpContext);

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Ticket!.Principal.Claims.Should().Contain(claim =>
            claim.Type == ClaimTypes.NameIdentifier && claim.Value == CallerConstants.MaintenanceAccountUserId);
        result.Ticket.Principal.Claims.Should().Contain(claim =>
            claim.Type == ClaimTypes.Role && claim.Value == UserRoles.ServiceAccount);
        result.Ticket.Principal.Claims.Should().Contain(claim =>
            claim.Type == ClaimTypes.UserData && claim.Value == UserFeatureSets.Basic);
        result.Ticket.Principal.Claims.Should().Contain(claim =>
            claim.Type == ClaimTypes.UserData && claim.Value == UserFeatureSets.Pro);
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndWrongSignature_ThenReturnsFailure()
    {
        _httpContext.Request.Headers.Add(HttpHeaders.HmacSignature, "asignature");
        await _handler.InitializeAsync(new AuthenticationScheme(HMACAuthenticationHandler.AuthenticationScheme, null,
            typeof(HMACAuthenticationHandler)), _httpContext);

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        _recorder.Verify(rec => rec.Audit(It.IsAny<ICallContext>(), AuditingConstants.HMACAuthenticationFailed,
            Resources.HMACAuthenticationHandler_FailedAuthentication));
        result.Failure.Should().BeOfType<Exception>()
            .Which.Message.Should()
            .Be(Resources.HMACAuthenticationHandler_WrongSignature);
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndSignatureMatches_ThenReturnsAuthenticated()
    {
        var body = new byte[] { 0x01 };
        var signature = new HMACSigner(body, "asecret").Sign();
        _httpContext.Request.Headers.Add(HttpHeaders.HmacSignature, signature);
        _httpContext.Request.Body = new MemoryStream(body);
        await _handler.InitializeAsync(new AuthenticationScheme(HMACAuthenticationHandler.AuthenticationScheme, null,
            typeof(HMACAuthenticationHandler)), _httpContext);

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Ticket!.Principal.Claims.Should().Contain(claim =>
            claim.Type == ClaimTypes.NameIdentifier && claim.Value == CallerConstants.MaintenanceAccountUserId);
        result.Ticket.Principal.Claims.Should().Contain(claim =>
            claim.Type == ClaimTypes.Role && claim.Value == UserRoles.ServiceAccount);
        result.Ticket.Principal.Claims.Should().Contain(claim =>
            claim.Type == ClaimTypes.UserData && claim.Value == UserFeatureSets.Basic);
        result.Ticket.Principal.Claims.Should().Contain(claim =>
            claim.Type == ClaimTypes.UserData && claim.Value == UserFeatureSets.Pro);
        _recorder.Verify(rec => rec.Audit(It.IsAny<ICallContext>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
    }
}