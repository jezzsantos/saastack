using System.Net;
using System.Text;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Endpoints;
using Infrastructure.Web.Api.Common.Pipeline;
using Infrastructure.Web.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Moq;
using Xunit;
using JsonOptions = Microsoft.AspNetCore.Http.Json.JsonOptions;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Web.Api.Common.UnitTests.Endpoints;

[Trait("Category", "Unit")]
public class ContentNegotiationFilterSpec
{
    private readonly ContentNegotiationFilter _filter;

    public ContentNegotiationFilterSpec()
    {
        _filter = new ContentNegotiationFilter();
    }

    [Fact]
    public async Task WhenInvokeAsyncWithNullResponse_ThenReturnsNull()
    {
        var context = new DefaultEndpointFilterInvocationContext(new DefaultHttpContext());
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>());

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeNull();
    }

    [Fact]
    public async Task WhenInvokeAsyncWithNakedObjectResponse_ThenReturnsJsonContentAsOk()
    {
        var response = new TestResponse();
        var httpContext = SetupHttpContext();
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<JsonHttpResult<object>>();
        result.As<JsonHttpResult<object>>()
            .ContentType.Should().BeNull();
        result.As<JsonHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<JsonHttpResult<object>>()
            .Value.Should().Be(response);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithNakedEmptyStringResponse_ThenReturnsJsonContentAsOk()
    {
        var response = string.Empty;
        var httpContext = SetupHttpContext();
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<NoContent>();
        result.As<NoContent>()
            .StatusCode.Should().Be((int)HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithIValueHttpResultResponse_ThenReturnsJsonContentAsOk()
    {
        var payload = new TestResponse();
        var response = Results.Ok(payload);
        var httpContext = SetupHttpContext();
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<JsonHttpResult<object>>();
        result.As<JsonHttpResult<object>>()
            .ContentType.Should().BeNull();
        result.As<JsonHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<JsonHttpResult<object>>()
            .Value.Should().Be(payload);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithStreamResultResponse_ThenReturnsJsonContentAsOk()
    {
        using var stream = new MemoryStream();
        var response = Results.Stream(stream);
        var httpContext = SetupHttpContext();
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<FileStreamHttpResult>();
        result.As<FileStreamHttpResult>()
            .ContentType.Should().Be(HttpConstants.ContentTypes.OctetStream);
        result.As<FileStreamHttpResult>()
            .FileStream.Should().BeSameAs(stream);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithEmptyProblemResultResponse_ThenReturnsJsonContentAsInternalServerError()
    {
        var response = Results.Problem();
        var httpContext = SetupHttpContext();
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<JsonHttpResult<object>>();
        result.As<JsonHttpResult<object>>()
            .ContentType.Should().Be(HttpConstants.ContentTypes.JsonProblem);
        result.As<JsonHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        result.As<JsonHttpResult<object>>().Value.As<ProblemDetails>().Status.Should().Be(500);
        result.As<JsonHttpResult<object>>().Value.As<ProblemDetails>().Title.Should()
            .Be("An error occurred while processing your request.");
        result.As<JsonHttpResult<object>>().Value.As<ProblemDetails>().Type.Should()
            .Be("https://tools.ietf.org/html/rfc9110#section-15.6.1");
    }

    [Fact]
    public async Task WhenInvokeAsyncWithProblemResultResponse_ThenReturnsJsonContentAsError()
    {
        var problem = new ProblemDetails
        {
            Type = "atype",
            Title = "atitle",
            Status = 999
        };
        var response = Results.Problem(problem);
        var httpContext = SetupHttpContext();
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<JsonHttpResult<object>>();
        result.As<JsonHttpResult<object>>()
            .ContentType.Should().Be(HttpConstants.ContentTypes.JsonProblem);
        result.As<JsonHttpResult<object>>()
            .StatusCode.Should().Be(999);
        result.As<JsonHttpResult<object>>().Value.As<ProblemDetails>().Status.Should().Be(999);
        result.As<JsonHttpResult<object>>().Value.As<ProblemDetails>().Title.Should().Be("atitle");
        result.As<JsonHttpResult<object>>().Value.As<ProblemDetails>().Type.Should().Be("atype");
    }

    [Fact]
    public async Task WhenInvokeAsyncWithValidationProblemResultResponse_ThenReturnsJsonContentAsBadRequest()
    {
        var errors = new Dictionary<string, string[]>
        {
            { "aname", new[] { "avalue1", "avalue2", "avalue3" } }
        };
        var response = Results.ValidationProblem(errors, type: "atype", title: "atitle", statusCode: 999);
        var httpContext = SetupHttpContext();
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<JsonHttpResult<object>>();
        result.As<JsonHttpResult<object>>()
            .ContentType.Should().Be(HttpConstants.ContentTypes.JsonProblem);
        result.As<JsonHttpResult<object>>()
            .StatusCode.Should().Be(999);
        result.As<JsonHttpResult<object>>().Value.As<HttpValidationProblemDetails>().Status.Should().Be(999);
        result.As<JsonHttpResult<object>>().Value.As<HttpValidationProblemDetails>().Title.Should().Be("atitle");
        result.As<JsonHttpResult<object>>().Value.As<HttpValidationProblemDetails>().Type.Should().Be("atype");
        result.As<JsonHttpResult<object>>().Value.As<HttpValidationProblemDetails>().Errors.Should()
            .BeEquivalentTo(errors);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithNoContentResponse_ThenReturnsJsonContentAsOk()
    {
        var response = Results.NoContent();
        var httpContext = SetupHttpContext();
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<NoContent>();
        result.As<NoContent>()
            .StatusCode.Should().Be((int)HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithOtherEmptyIResultResponse_ThenReturnsJsonContentAsOk()
    {
        var response = Results.Ok();
        var httpContext = SetupHttpContext();
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<Ok>();
        result.As<Ok>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithNullValueIResultResponse_ThenReturnsJsonContentAsOk()
    {
        var response = TypedResults.Ok((string)null!);
        var httpContext = SetupHttpContext();
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<Ok<string>>();
        result.As<Ok<string>>()
            .StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithNakedObjectResponseAndAcceptEverything_ThenReturnsJson()
    {
        var response = new TestResponse();
        var httpContext = SetupHttpContext(FormatMechanism.AcceptHeader(HttpConstants.ContentTypes.Everything));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<JsonHttpResult<object>>();
        result.As<JsonHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<JsonHttpResult<object>>()
            .Value.Should().Be(response);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithIResultResponseAndAcceptEverything_ThenReturnsJson()
    {
        var payload = new TestResponse();
        var response = Results.Ok(payload);
        var httpContext = SetupHttpContext(FormatMechanism.AcceptHeader(HttpConstants.ContentTypes.Everything));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<JsonHttpResult<object>>();
        result.As<JsonHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<JsonHttpResult<object>>()
            .Value.Should().Be(payload);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithNakedObjectResponseAndAcceptJson_ThenReturnsJson()
    {
        var response = new TestResponse();
        var httpContext = SetupHttpContext(FormatMechanism.AcceptHeader(HttpConstants.ContentTypes.Json));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<JsonHttpResult<object>>();
        result.As<JsonHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<JsonHttpResult<object>>()
            .Value.Should().Be(response);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithIResultResponseAndAcceptJson_ThenReturnsJson()
    {
        var payload = new TestResponse();
        var response = Results.Ok(payload);
        var httpContext = SetupHttpContext(FormatMechanism.AcceptHeader(HttpConstants.ContentTypes.Json));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<JsonHttpResult<object>>();
        result.As<JsonHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<JsonHttpResult<object>>()
            .Value.Should().Be(payload);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithNakedObjectResponseAndAcceptXml_ThenReturnsXml()
    {
        var response = new TestResponse();
        var httpContext = SetupHttpContext(FormatMechanism.AcceptHeader(HttpConstants.ContentTypes.Xml));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<XmlHttpResult<object>>();
        result.As<XmlHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<XmlHttpResult<object>>()
            .Value.Should().Be(response);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithIResultResponseAndAcceptXml_ThenReturnsXml()
    {
        var payload = new TestResponse();
        var response = Results.Ok(payload);
        var httpContext = SetupHttpContext(FormatMechanism.AcceptHeader(HttpConstants.ContentTypes.Xml));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<XmlHttpResult<object>>();
        result.As<XmlHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<XmlHttpResult<object>>()
            .Value.Should().Be(payload);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithNakedObjectResponseAndQueryStringFormatJson_ThenReturnsJson()
    {
        var response = new TestResponse();
        var httpContext = SetupHttpContext(FormatMechanism.QueryString(HttpConstants.ContentTypeFormatters.Json));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<JsonHttpResult<object>>();
        result.As<JsonHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<JsonHttpResult<object>>()
            .Value.Should().Be(response);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithIResultResponseAndQueryStringFormatJson_ThenReturnsJson()
    {
        var payload = new TestResponse();
        var response = Results.Ok(payload);
        var httpContext = SetupHttpContext(FormatMechanism.QueryString(HttpConstants.ContentTypeFormatters.Json));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<JsonHttpResult<object>>();
        result.As<JsonHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<JsonHttpResult<object>>()
            .Value.Should().Be(payload);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithNakedObjectResponseAndQueryStringFormatXml_ThenReturnsXml()
    {
        var response = new TestResponse();
        var httpContext = SetupHttpContext(FormatMechanism.QueryString(HttpConstants.ContentTypeFormatters.Xml));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<XmlHttpResult<object>>();
        result.As<XmlHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<XmlHttpResult<object>>()
            .Value.Should().Be(response);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithIResultResponseAndQueryStringFormatXml_ThenReturnsXml()
    {
        var payload = new TestResponse();
        var response = Results.Ok(payload);
        var httpContext = SetupHttpContext(FormatMechanism.QueryString(HttpConstants.ContentTypeFormatters.Xml));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<XmlHttpResult<object>>();
        result.As<XmlHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<XmlHttpResult<object>>()
            .Value.Should().Be(payload);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithNakedObjectResponseAndFormBodyFormatJson_ThenReturnsJson()
    {
        var response = new TestResponse();
        var httpContext = SetupHttpContext(FormatMechanism.FormBody(HttpConstants.ContentTypeFormatters.Json));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<JsonHttpResult<object>>();
        result.As<JsonHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<JsonHttpResult<object>>()
            .Value.Should().Be(response);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithIResultResponseAndFormBodyFormatJson_ThenReturnsJson()
    {
        var payload = new TestResponse();
        var response = Results.Ok(payload);
        var httpContext = SetupHttpContext(FormatMechanism.FormBody(HttpConstants.ContentTypeFormatters.Json));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<JsonHttpResult<object>>();
        result.As<JsonHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<JsonHttpResult<object>>()
            .Value.Should().Be(payload);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithNakedObjectResponseAndFormBodyFormatXml_ThenReturnsXml()
    {
        var response = new TestResponse();
        var httpContext = SetupHttpContext(FormatMechanism.FormBody(HttpConstants.ContentTypeFormatters.Xml));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<XmlHttpResult<object>>();
        result.As<XmlHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<XmlHttpResult<object>>()
            .Value.Should().Be(response);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithIResultResponseAndFormBodyFormatXml_ThenReturnsXml()
    {
        var payload = new TestResponse();
        var response = Results.Ok(payload);
        var httpContext = SetupHttpContext(FormatMechanism.FormBody(HttpConstants.ContentTypeFormatters.Xml));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<XmlHttpResult<object>>();
        result.As<XmlHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<XmlHttpResult<object>>()
            .Value.Should().Be(payload);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithNakedObjectResponseAndJsonBodyFormatXml_ThenReturnsXml()
    {
        var response = new TestResponse();
        var httpContext = SetupHttpContext(FormatMechanism.JsonBody(HttpConstants.ContentTypeFormatters.Xml));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<XmlHttpResult<object>>();
        result.As<XmlHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<XmlHttpResult<object>>()
            .Value.Should().Be(response);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithIResultResponseAndJsonBodyFormatXml_ThenReturnsXml()
    {
        var payload = new TestResponse();
        var response = Results.Ok(payload);
        var httpContext = SetupHttpContext(FormatMechanism.JsonBody(HttpConstants.ContentTypeFormatters.Xml));
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<XmlHttpResult<object>>();
        result.As<XmlHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<XmlHttpResult<object>>()
            .Value.Should().Be(payload);
    }

    [Fact]
    public async Task WhenInvokeAsyncWithIResultResponseAndNoJsonBody_ThenReturnsJson()
    {
        var payload = new TestResponse();
        var response = Results.Ok(payload);
        var httpContext = SetupHttpContext(FormatMechanism.None());
        var context = new DefaultEndpointFilterInvocationContext(httpContext.Object);
        var next = new EndpointFilterDelegate(_ => new ValueTask<object?>(response));

        var result = await _filter.InvokeAsync(context, next);

        result.Should().BeOfType<JsonHttpResult<object>>();
        result.As<JsonHttpResult<object>>()
            .StatusCode.Should().Be((int)HttpStatusCode.OK);
        result.As<JsonHttpResult<object>>()
            .Value.Should().Be(payload);
    }

    private static Mock<HttpContext> SetupHttpContext(FormatMechanism? mechanism = null)
    {
        var jsonOptions = new Mock<IOptions<JsonOptions>>();
        jsonOptions.Setup(jo => jo.Value)
            .Returns(new JsonOptions());

        var httpRequest = new Mock<HttpRequest>();
        httpRequest.Setup(hr => hr.Headers)
            .Returns(mechanism is null || mechanism.AcceptHeaderFormat.HasNoValue()
                ? new HeaderDictionary()
                : new HeaderDictionary(new Dictionary<string, StringValues>
                {
                    { HttpConstants.Headers.Accept, new StringValues(mechanism.AcceptHeaderFormat) }
                }));
        httpRequest.Setup(hr => hr.Query)
            .Returns(mechanism is null || mechanism.QueryStringFormat.HasNoValue()
                ? new QueryCollection()
                : new QueryCollection(new Dictionary<string, StringValues>
                {
                    { HttpConstants.QueryParams.Format, new StringValues(mechanism.QueryStringFormat) }
                }));
        if (mechanism is not null)
        {
            if (mechanism.FormBodyFormat.HasValue())
            {
                httpRequest.Setup(hr => hr.HasFormContentType).Returns(true);
                httpRequest.Setup(hr => hr.Form).Returns(new FormCollection(new Dictionary<string, StringValues>
                {
                    { "format", new StringValues(mechanism.FormBodyFormat) }
                }));
            }
            else if (mechanism.JsonBodyFormat.HasValue())
            {
                httpRequest.Setup(hr => hr.ContentType).Returns(HttpConstants.ContentTypes.Json);
                httpRequest.Setup(hr => hr.Body)
                    .Returns(new MemoryStream(
                        Encoding.UTF8.GetBytes($"{{\"format\":\"{mechanism.JsonBodyFormat}\"}}")));
            }
            else
            {
                httpRequest.Setup(hr => hr.ContentType).Returns(HttpConstants.ContentTypes.Json);
                httpRequest.Setup(hr => hr.Body)
                    .Returns(new MemoryStream(Array.Empty<byte>()));
            }
        }

        var httpContext = new Mock<HttpContext>();
        httpContext.Setup(hc => hc.RequestServices.GetService(It.IsAny<Type>()))
            .Returns(jsonOptions.Object);
        httpRequest.Setup(hr => hr.HttpContext).Returns(httpContext.Object);
        httpContext.Setup(hc => hc.Request)
            .Returns(httpRequest.Object);

        return httpContext;
    }

    private class FormatMechanism
    {
        private FormatMechanism(string? accept, string? queryString, string? formBody, string? jsonBody)
        {
            AcceptHeaderFormat = accept;
            QueryStringFormat = queryString;
            FormBodyFormat = formBody;
            JsonBodyFormat = jsonBody;
        }

        public string? AcceptHeaderFormat { get; }

        public string? FormBodyFormat { get; }

        public string? JsonBodyFormat { get; }

        public string? QueryStringFormat { get; }

        public static FormatMechanism AcceptHeader(string value)
        {
            return new FormatMechanism(value, null, null, null);
        }

        public static FormatMechanism FormBody(string value)
        {
            return new FormatMechanism(null, null, value, null);
        }

        public static FormatMechanism JsonBody(string value)
        {
            return new FormatMechanism(null, null, null, value);
        }

        public static FormatMechanism None()
        {
            return new FormatMechanism(null, null, null, null);
        }

        public static FormatMechanism QueryString(string value)
        {
            return new FormatMechanism(null, value, null, null);
        }
    }
}