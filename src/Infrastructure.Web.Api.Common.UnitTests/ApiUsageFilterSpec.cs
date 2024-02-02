using Application.Interfaces;
using Common;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Health;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests;

[Trait("Category", "Unit")]
public class ApiUsageFilterSpec
{
    private readonly ApiUsageFilter _filter;
    private readonly Mock<IRecorder> _recorder;

    public ApiUsageFilterSpec()
    {
        _recorder = new Mock<IRecorder>();
        var caller = new Mock<ICallerContext>();
        caller.Setup(cc => cc.CallerId).Returns("acallerid");
        var callerContextFactory = new Mock<ICallerContextFactory>();
        callerContextFactory.Setup(ccf => ccf.Create())
            .Returns(caller.Object);

        _filter = new ApiUsageFilter(_recorder.Object, callerContextFactory.Object);
    }

    [Fact]
    public async Task WhenInvokeAndNotEnoughArguments_ThenDoesNothing()
    {
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext());
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>());

        await _filter.InvokeAsync(context, next);

        _recorder.Verify(
            rec => rec.TrackUsage(It.IsAny<ICallContext?>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAndSecondArgumentIsNotARequestType_ThenDoesNothing()
    {
        var args = new object[] { "anarg1", "anarg2" };
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), args);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>());

        await _filter.InvokeAsync(context, next);

        _recorder.Verify(
            rec => rec.TrackUsage(It.IsAny<ICallContext?>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAndRequestTypeIsIgnoredRequest_ThenDoesNothing()
    {
        var args = new object[] { "anarg1", new HealthCheckRequest() };
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext(), args);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>());

        await _filter.InvokeAsync(context, next);

        _recorder.Verify(
            rec => rec.TrackUsage(It.IsAny<ICallContext?>(), It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>()), Times.Never);
    }

    [Fact]
    public async Task WhenInvokeAndRequestTypeIsNotIgnored_ThenTracksUsage()
    {
        var args = new object[] { "anarg1", new GetCallerWithTokenOrAPIKeyTestingOnlyRequest() };
        var httpContext = new DefaultHttpContext
        {
            Request = { Method = "amethod", Path = "/apath" },
            Response = { StatusCode = 200 }
        };
        httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, null, "aroute"));
        var context = new DefaultEndpointFilterInvocationContext(httpContext, args);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>());

        await _filter.InvokeAsync(context, next);

        _recorder.Verify(
            rec => rec.TrackUsage(It.IsAny<ICallContext?>(), UsageConstants.Events.Api.HttpRequestRequested,
                It.Is<Dictionary<string, object>>(dic =>
                    dic.Count == 5
                    && dic[UsageConstants.Properties.EndPoint].As<string>()
                    == nameof(GetCallerWithTokenOrAPIKeyTestingOnlyRequest).ToLower()
                    && dic[UsageConstants.Properties.UsedById].As<string>() == "acallerid"
                    && dic[UsageConstants.Properties.HttpRoute].As<string>() == "aroute"
                    && dic[UsageConstants.Properties.HttpPath].As<string>() == "/apath"
                    && dic[UsageConstants.Properties.HttpMethod].As<string>() == "amethod"
                )));
        _recorder.Verify(
            rec => rec.TrackUsage(It.IsAny<ICallContext?>(), UsageConstants.Events.Api.HttpRequestResponded,
                It.Is<Dictionary<string, object>>(dic =>
                    dic.Count == 7
                    && dic[UsageConstants.Properties.EndPoint].As<string>()
                    == nameof(GetCallerWithTokenOrAPIKeyTestingOnlyRequest).ToLower()
                    && dic[UsageConstants.Properties.UsedById].As<string>() == "acallerid"
                    && dic[UsageConstants.Properties.HttpRoute].As<string>() == "aroute"
                    && dic[UsageConstants.Properties.HttpPath].As<string>() == "/apath"
                    && dic[UsageConstants.Properties.HttpMethod].As<string>() == "amethod"
                    && dic[UsageConstants.Properties.HttpStatusCode].As<int>() == 0
                    && dic[UsageConstants.Properties.Duration].As<double>() > 0
                )));
    }

    [Fact]
    public async Task WhenInvokeAndRequestTypeIncludesAResourceId_ThenTracksUsage()
    {
        var args = new object[] { "anarg1", new TestRequest { Id = "aresourceid" } };
        var httpContext = new DefaultHttpContext
        {
            Request = { Method = "amethod", Path = "/apath" }
        };
        httpContext.SetEndpoint(new Endpoint(_ => Task.CompletedTask, null, "aroute"));
        var context = new DefaultEndpointFilterInvocationContext(httpContext, args);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>());

        await _filter.InvokeAsync(context, next);

        _recorder.Verify(
            rec => rec.TrackUsage(It.IsAny<ICallContext?>(), UsageConstants.Events.Api.HttpRequestRequested,
                It.Is<Dictionary<string, object>>(dic =>
                    dic.Count == 6
                    && dic[UsageConstants.Properties.EndPoint].As<string>() == nameof(TestRequest).ToLower()
                    && dic[UsageConstants.Properties.UsedById].As<string>() == "acallerid"
                    && dic[UsageConstants.Properties.HttpRoute].As<string>() == "aroute"
                    && dic[UsageConstants.Properties.HttpPath].As<string>() == "/apath"
                    && dic[UsageConstants.Properties.HttpMethod].As<string>() == "amethod"
                    && dic[UsageConstants.Properties.ResourceId].As<string>() == "aresourceid"
                )));
        _recorder.Verify(
            rec => rec.TrackUsage(It.IsAny<ICallContext?>(), UsageConstants.Events.Api.HttpRequestResponded,
                It.Is<Dictionary<string, object>>(dic =>
                    dic.Count == 8
                    && dic[UsageConstants.Properties.EndPoint].As<string>() == nameof(TestRequest).ToLower()
                    && dic[UsageConstants.Properties.UsedById].As<string>() == "acallerid"
                    && dic[UsageConstants.Properties.HttpRoute].As<string>() == "aroute"
                    && dic[UsageConstants.Properties.HttpPath].As<string>() == "/apath"
                    && dic[UsageConstants.Properties.HttpMethod].As<string>() == "amethod"
                    && dic[UsageConstants.Properties.ResourceId].As<string>() == "aresourceid"
                    && dic[UsageConstants.Properties.HttpStatusCode].As<int>() == 0
                    && dic[UsageConstants.Properties.Duration].As<double>() > 0
                )));
    }
}