using System.Text.Encodings.Web;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
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
public class APIKeyAuthenticationHandlerSpec
{
    private readonly APIKeyAuthenticationHandler _handler;
    private readonly DefaultHttpContext _httpContext;
    private readonly Mock<IIdentityService> _identityService;
    private readonly Mock<IRecorder> _recorder;

    public APIKeyAuthenticationHandlerSpec()
    {
        var clock = new Mock<ISystemClock>();
        var options = new Mock<IOptionsMonitor<APIKeyOptions>>();
        options.Setup(x => x.Get(It.IsAny<string>()))
            .Returns(new APIKeyOptions());
        var serviceCollection = new ServiceCollection();
        _recorder = new Mock<IRecorder>();
        serviceCollection.AddSingleton(_recorder.Object);
        var caller = new Mock<ICallerContext>();
        var callerFactory = new Mock<ICallerContextFactory>();
        callerFactory.Setup(cf => cf.Create()).Returns(caller.Object);
        serviceCollection.AddSingleton(callerFactory.Object);
        _identityService = new Mock<IIdentityService>();
        serviceCollection.AddSingleton(_identityService.Object);
        _httpContext = new DefaultHttpContext
        {
            Request =
            {
                IsHttps = true
            },
            RequestServices = serviceCollection.BuildServiceProvider()
        };

        _handler = new APIKeyAuthenticationHandler(options.Object, new LoggerFactory(), UrlEncoder.Default,
            clock.Object);
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndConnectionNotSecure_ThenReturnsFailure()
    {
        _httpContext.Request.IsHttps = false;
        _handler.InitializeAsync(new AuthenticationScheme(APIKeyAuthenticationHandler.AuthenticationScheme, null,
            typeof(APIKeyAuthenticationHandler)), _httpContext).GetAwaiter().GetResult();

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().BeOfType<Exception>()
            .Which.Message.Should().Be(Resources.AuthenticationHandler_NotHttps);
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndNoApiKeyInRequest_ThenReturnsFailure()
    {
        _handler.InitializeAsync(new AuthenticationScheme(APIKeyAuthenticationHandler.AuthenticationScheme, null,
            typeof(APIKeyAuthenticationHandler)), _httpContext).GetAwaiter().GetResult();

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().BeOfType<Exception>()
            .Which.Message.Should()
            .Be(Resources.APIKeyAuthenticationHandler_MissingParameter.Format(HttpQueryParams.APIKey));
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndUnknownApiKey_ThenReturnsFailure()
    {
        _identityService.Setup(ids =>
                ids.FindMembershipsForAPIKeyAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<EndUserWithMemberships>, Error>>(Optional<EndUserWithMemberships>
                .None));
        _httpContext.Request.QueryString = QueryString.Create(HttpQueryParams.APIKey, "anapikey");
        await _handler.InitializeAsync(new AuthenticationScheme(APIKeyAuthenticationHandler.AuthenticationScheme, null,
            typeof(APIKeyAuthenticationHandler)), _httpContext);

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        _recorder.Verify(rec => rec.Audit(It.IsAny<ICallContext>(), AuditingConstants.APIKeyAuthenticationFailed,
            Resources.APIKeyAuthenticationHandler_FailedAuthentication));
        result.Failure.Should().BeOfType<Exception>()
            .Which.Message.Should()
            .Be(Resources.AuthenticationHandler_Failed);
        _identityService.Verify(ids =>
            ids.FindMembershipsForAPIKeyAsync(It.IsAny<ICallerContext>(), "anapikey", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndKnownUserFromApiKeyInQueryString_ThenReturnsAuthenticated()
    {
        var user = new EndUserWithMemberships
        {
            Id = "anid"
        };
        _identityService.Setup(ids =>
                ids.FindMembershipsForAPIKeyAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<EndUserWithMemberships>, Error>>(user.ToOptional()));
        _httpContext.Request.QueryString = QueryString.Create(HttpQueryParams.APIKey, "anapikey");
        await _handler.InitializeAsync(new AuthenticationScheme(APIKeyAuthenticationHandler.AuthenticationScheme, null,
            typeof(APIKeyAuthenticationHandler)), _httpContext);

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Ticket!.Principal.Claims.Should().Contain(claim =>
            claim.Type == AuthenticationConstants.Claims.ForId
            && claim.Value == "anid");
        _recorder.Verify(rec => rec.Audit(It.IsAny<ICallContext>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _identityService.Verify(ids =>
            ids.FindMembershipsForAPIKeyAsync(It.IsAny<ICallerContext>(), "anapikey", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenHandleAuthenticateAsyncAndKnownUserFromApiKeyInBasicAuth_ThenReturnsAuthenticated()
    {
        var user = new EndUserWithMemberships
        {
            Id = "anid"
        };
        _identityService.Setup(ids =>
                ids.FindMembershipsForAPIKeyAsync(It.IsAny<ICallerContext>(), It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<EndUserWithMemberships>, Error>>(user.ToOptional()));
        _httpContext.Request.Headers[HttpHeaders.Authorization] =
            $"Basic {Convert.ToBase64String("anapikey:"u8.ToArray())}";
        await _handler.InitializeAsync(new AuthenticationScheme(APIKeyAuthenticationHandler.AuthenticationScheme, null,
            typeof(APIKeyAuthenticationHandler)), _httpContext);

        var result = await _handler.AuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Ticket!.Principal.Claims.Should().Contain(claim =>
            claim.Type == AuthenticationConstants.Claims.ForId
            && claim.Value == "anid");
        _recorder.Verify(rec => rec.Audit(It.IsAny<ICallContext>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        _identityService.Verify(ids =>
            ids.FindMembershipsForAPIKeyAsync(It.IsAny<ICallerContext>(), "anapikey", It.IsAny<CancellationToken>()));
    }
}