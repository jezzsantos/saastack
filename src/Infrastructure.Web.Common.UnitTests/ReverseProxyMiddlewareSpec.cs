using System.Net;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Moq.Protected;
using Xunit;

namespace Infrastructure.Web.Common.UnitTests;

[Trait("Category", "Unit")]
public class ReverseProxyMiddlewareSpec
{
    private readonly Mock<IHttpClientFactory> _httpClientFactory;
    private readonly Mock<HttpMessageHandler> _httpMessageHandler;
    private readonly ReverseProxyMiddleware _middleware;
    private readonly Mock<RequestDelegate> _next;

    public ReverseProxyMiddlewareSpec()
    {
        var settings = new Mock<IHostSettings>();
        settings.Setup(s => s.GetApiHost1BaseUrl())
            .Returns("https://localhost/abaseurl");
        _httpMessageHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        var httpClient = new HttpClient(_httpMessageHandler.Object);
        _httpClientFactory = new Mock<IHttpClientFactory>();
        _httpClientFactory.Setup(hcf => hcf.CreateClient(It.IsAny<string>()))
            .Returns(httpClient);
        _next = new Mock<RequestDelegate>();

        _middleware = new ReverseProxyMiddleware(_next.Object, settings.Object, _httpClientFactory.Object);
    }

    [Fact]
    public async Task WhenInvokeAsyncAndIsAnEndpoint_ThenContinuesPipeline()
    {
        var context = new DefaultHttpContext();
        context.SetEndpoint(new Endpoint(_ => Task.CompletedTask, null, "aroute"));

        await _middleware.InvokeAsync(context);

        _next.Verify(n => n.Invoke(context));
        _httpClientFactory.Verify(hcf => hcf.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAsyncAndIsNotAnAPICall_ThenContinuesPipeline()
    {
        var context = new DefaultHttpContext
        {
            Request = { PathBase = new PathString("/apath") }
        };

        await _middleware.InvokeAsync(context);

        _next.Verify(n => n.Invoke(context));
        _httpClientFactory.Verify(hcf => hcf.CreateClient(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAsyncAndIsAnAPICall_ThenForwardsToBackend()
    {
        var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK
        };
        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
        var context = new DefaultHttpContext
        {
            Request =
            {
                Method = HttpMethods.Post,
                PathBase = new PathString("/api/apath")
            }
        };

        await _middleware.InvokeAsync(context);

        _next.Verify(n => n.Invoke(context), Times.Never);
        _httpClientFactory.Verify(hcf => hcf.CreateClient(It.IsAny<string>()));
        _httpMessageHandler.Protected()
            .Verify("SendAsync", Times.Once(), ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>());
    }
}