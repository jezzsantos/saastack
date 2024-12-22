using Application.Interfaces;
using Common;
using FluentAssertions;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Endpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests.Endpoints;

[Trait("Category", "Unit")]
public class HttpRecordingFilterSpec
{
    private readonly HttpRecordingFilter _filter;
    private readonly Mock<IRecorder> _recorder;

    public HttpRecordingFilterSpec()
    {
        _recorder = new Mock<IRecorder>();
        var caller = new Mock<ICallerContext>();
        caller.Setup(cc => cc.CallerId).Returns("acallerid");
        var callerContextFactory = new Mock<ICallerContextFactory>();
        callerContextFactory.Setup(ccf => ccf.Create())
            .Returns(caller.Object);

        _filter = new HttpRecordingFilter(_recorder.Object, callerContextFactory.Object);
    }

    [Fact]
    public async Task WhenInvokeAndNullResponse_ThenJustTracesRequest()
    {
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext
        {
            Request =
            {
                Method = "amethod",
                Path = "/apath",
                Headers = { Accept = "anaccept" }
            }
        });
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>());

        await _filter.InvokeAsync(context, next);

        _recorder.Verify(
            rec => rec.TraceInformation(It.IsAny<ICallContext?>(), @"{Request}: Received",
                It.Is<object[]>(arr =>
                    arr.Length == 1
                    && arr[0].As<string>() == "amethod /apath (anaccept)"
                )));
    }

    [Fact]
    public async Task WhenInvokeAndResponseIsNotAnOkResult_ThenTracesRequestAndSuccessResponse()
    {
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext
        {
            Request =
            {
                Method = "amethod",
                Path = "/apath",
                Headers = { Accept = "anaccept" }
            },
            Response =
            {
                StatusCode = 499
            }
        });
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(Results.Ok()));

        await _filter.InvokeAsync(context, next);

        _recorder.Verify(
            rec => rec.TraceInformation(It.IsAny<ICallContext?>(), @"{Request}: Received",
                It.Is<object[]>(arr =>
                    arr.Length == 1
                    && arr[0].As<string>() == "amethod /apath (anaccept)"
                )));
        _recorder.Verify(
            rec => rec.TraceInformation(It.IsAny<ICallContext?>(), @"{Request}: {Result}",
                It.Is<object[]>(arr =>
                    arr.Length == 2
                    && arr[0].As<string>() == "amethod /apath (anaccept)"
                    && arr[1].As<string>() == "200 - OK"
                )));
    }

    [Fact]
    public async Task WhenInvokeAndResponseIsNotAResultWithStatusCode_ThenTracesRequestAndSuccessResponse()
    {
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext
        {
            Request =
            {
                Method = "amethod",
                Path = "/apath",
                Headers = { Accept = "anaccept" }
            },
            Response =
            {
                StatusCode = 499
            }
        });
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(Results.NoContent()));

        await _filter.InvokeAsync(context, next);

        _recorder.Verify(
            rec => rec.TraceInformation(It.IsAny<ICallContext?>(), @"{Request}: Received",
                It.Is<object[]>(arr =>
                    arr.Length == 1
                    && arr[0].As<string>() == "amethod /apath (anaccept)"
                )));
        _recorder.Verify(
            rec => rec.TraceInformation(It.IsAny<ICallContext?>(), @"{Request}: {Result}",
                It.Is<object[]>(arr =>
                    arr.Length == 2
                    && arr[0].As<string>() == "amethod /apath (anaccept)"
                    && arr[1].As<string>() == "204 - NoContent"
                )));
    }

    [Fact]
    public async Task WhenInvokeAndResponseIsNotAResultWithNoStatusCode_ThenTracesRequestAndFailureResponse()
    {
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext
        {
            Request =
            {
                Method = "amethod",
                Path = "/apath",
                Headers = { Accept = "anaccept" }
            },
            Response =
            {
                StatusCode = 499
            }
        });
        var next = new EndpointFilterDelegate(_ =>
            new ValueTask<object?>(Results.Stream(new MemoryStream())));

        await _filter.InvokeAsync(context, next);

        _recorder.Verify(
            rec => rec.TraceInformation(It.IsAny<ICallContext?>(), @"{Request}: Received",
                It.Is<object[]>(arr =>
                    arr.Length == 1
                    && arr[0].As<string>() == "amethod /apath (anaccept)"
                )));
        _recorder.Verify(
            rec => rec.TraceInformation(It.IsAny<ICallContext?>(), @"{Request}: {Result}",
                It.Is<object[]>(arr =>
                    arr.Length == 2
                    && arr[0].As<string>() == "amethod /apath (anaccept)"
                    && arr[1].As<string>() == "499 - 499"
                )));
    }

    [Fact]
    public async Task WhenInvokeAndResponseIsJsonHttpResultWithProblem_ThenTracesRequestAndFailureResponse()
    {
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext
        {
            Request =
            {
                Method = "amethod",
                Path = "/apath",
                Headers = { Accept = "anaccept" }
            },
            Response =
            {
                StatusCode = 499
            }
        });
        var next = new EndpointFilterDelegate(_ =>
            new ValueTask<object?>(Results.Json(new ProblemDetails
            {
                Status = 500,
                Title = "atitle"
            })));

        await _filter.InvokeAsync(context, next);

        _recorder.Verify(
            rec => rec.TraceInformation(It.IsAny<ICallContext?>(), @"{Request}: Received",
                It.Is<object[]>(arr =>
                    arr.Length == 1
                    && arr[0].As<string>() == "amethod /apath (anaccept)"
                )));
        _recorder.Verify(
            rec => rec.TraceError(It.IsAny<ICallContext?>(), @"{Request}: {Result}, problem: {Problem}",
                It.Is<object[]>(arr =>
                    arr.Length == 3
                    && arr[0].As<string>() == "amethod /apath (anaccept)"
                    && arr[1].As<string>() == "500 - InternalServerError"
                    && arr[2].As<string>()
                    == "{\r\n  \"type\": \"https://tools.ietf.org/html/rfc9110#section-15.6.1\",\r\n  \"title\": \"atitle\",\r\n  \"status\": 500\r\n}"
                )));
    }

    [Fact]
    public async Task WhenInvokeAndResponseIsHttpProblemDetails_ThenTracesRequestAndFailureResponse()
    {
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext
        {
            Request =
            {
                Method = "amethod",
                Path = "/apath",
                Headers = { Accept = "anaccept" }
            },
            Response =
            {
                StatusCode = 499
            }
        });
        var next = new EndpointFilterDelegate(_ =>
            new ValueTask<object?>(Results.Problem(type: "atype", title: "atitle", statusCode: 500)));

        await _filter.InvokeAsync(context, next);

        _recorder.Verify(
            rec => rec.TraceInformation(It.IsAny<ICallContext?>(), @"{Request}: Received",
                It.Is<object[]>(arr =>
                    arr.Length == 1
                    && arr[0].As<string>() == "amethod /apath (anaccept)"
                )));
        _recorder.Verify(
            rec => rec.TraceError(It.IsAny<ICallContext?>(), @"{Request}: {Result}, problem: {Problem}",
                It.Is<object[]>(arr =>
                    arr.Length == 3
                    && arr[0].As<string>() == "amethod /apath (anaccept)"
                    && arr[1].As<string>() == "500 - InternalServerError"
                    && arr[2].As<string>()
                    == "{\r\n  \"type\": \"atype\",\r\n  \"title\": \"atitle\",\r\n  \"status\": 500\r\n}"
                )));
    }
}