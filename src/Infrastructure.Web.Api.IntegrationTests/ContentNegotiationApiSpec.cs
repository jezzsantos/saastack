#if TESTINGONLY
using System.Net;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace Infrastructure.Web.Api.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class ContentNegotiationApiSpec : WebApiSpec<ApiHost1.Program>
{
    public ContentNegotiationApiSpec(WebApiSetup<ApiHost1.Program> setup) : base(setup)
    {
    }

    [Fact]
    public async Task WhenGetWithNoAccept_ThenReturnsJsonResponse()
    {
        var result = await Api.GetAsync(new ContentNegotiationsTestingOnlyRequest(),
            request => request.Headers.Remove(HttpConstants.Headers.Accept));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGetWithAcceptForUnsupported_ThenReturns415()
    {
        var result = await HttpApi.SendAsync(new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("testingonly/negotiations/get", UriKind.Relative),
            Headers = { { HttpConstants.Headers.Accept, "application/unsupported" } }
        });

        result.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task WhenGetWithAcceptForJson_ThenReturnsJsonResponse()
    {
        var result = await Api.GetAsync(new ContentNegotiationsTestingOnlyRequest(),
            request => request.Headers.Add(HttpConstants.Headers.Accept, HttpConstants.ContentTypes.Json));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGetWithAcceptForXml_ThenReturnsXmlResponse()
    {
        var result = await HttpApi.SendAsync(new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("testingonly/negotiations/get", UriKind.Relative),
            Headers = { { HttpConstants.Headers.Accept, HttpConstants.ContentTypes.Xml } }
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Content.ReadAsStringAsync();
        content.Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                            + "<StringMessageTestingOnlyResponse xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">"
                            + "<Message>amessage</Message>" + "</StringMessageTestingOnlyResponse>");
    }

    [Fact]
    public async Task WhenGetWithNoFormat_ThenReturnsJsonResponse()
    {
        var result = await Api.GetAsync(new ContentNegotiationsTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGetWithFormatForUnsupported_ThenReturns415()
    {
        var result = await HttpApi.SendAsync(new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("testingonly/negotiations/get?format=unsupported", UriKind.Relative)
        });

        result.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task WhenGetWithFormatForJson_ThenReturnsJsonResponse()
    {
        var result = await Api.GetAsync(new ContentNegotiationsTestingOnlyRequest { Format = "json" });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGetWithFormatForXml_ThenReturnsXmlResponse()
    {
        var result = await HttpApi.SendAsync(new HttpRequestMessage
        {
            Method = HttpMethod.Get,
            RequestUri = new Uri("testingonly/negotiations/get?format=xml", UriKind.Relative),
            Headers = { { HttpConstants.Headers.Accept, HttpConstants.ContentTypes.Xml } }
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Content.ReadAsStringAsync();
        content.Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                            + "<StringMessageTestingOnlyResponse xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">"
                            + "<Message>amessage</Message>" + "</StringMessageTestingOnlyResponse>");
    }
}
#endif