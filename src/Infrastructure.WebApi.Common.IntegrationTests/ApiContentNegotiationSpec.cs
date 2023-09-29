#if TESTINGONLY
using System.Net;
using ApiHost1;
using FluentAssertions;
using Infrastructure.WebApi.Interfaces.Operations.TestingOnly;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace Infrastructure.WebApi.Common.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class ApiContentNegotiationSpec : WebApiSpec<Program>
{
    public ApiContentNegotiationSpec(WebApiSetup<Program> setup) : base(setup)
    {
    }

    [Fact]
    public async Task WhenGetWithNoAccept_ThenReturnsJsonResponse()
    {
        var result = await Api.GetAsync<StringMessageTestingOnlyResponse>("testingonly/negotiations/get",
            request => request.Headers.Remove(HttpHeaders.Accept));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGetWithAcceptForUnsupported_ThenReturns415()
    {
        var result = await Api.GetAsync("/testingonly/negotiations/get",
            request => request.Headers.Add(HttpHeaders.Accept, "application/unsupported"));

        result.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        result.Content.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenGetWithAcceptForJson_ThenReturnsJsonResponse()
    {
        var result = await Api.GetAsync<StringMessageTestingOnlyResponse>("/testingonly/negotiations/get",
            request => request.Headers.Add(HttpHeaders.Accept, HttpContentTypes.Json));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGetWithAcceptForXml_ThenReturnsXmlResponse()
    {
        var result = await Api.GetAsync("/testingonly/negotiations/get",
            request => request.Headers.Add(HttpHeaders.Accept, HttpContentTypes.Xml));

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                   "<StringMessageTestingOnlyResponse xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
                                   "<Message>amessage</Message>" + "</StringMessageTestingOnlyResponse>");
    }

    [Fact]
    public async Task WhenGetWithNoFormat_ThenReturnsJsonResponse()
    {
        var result = await Api.GetAsync<StringMessageTestingOnlyResponse>("/testingonly/negotiations/get");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGetWithFormatForUnsupported_ThenReturns415()
    {
        var result = await Api.GetAsync("/testingonly/negotiations/get?format=unsupported");

        result.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        result.Content.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenGetWithFormatForJson_ThenReturnsJsonResponse()
    {
        var result = await Api.GetAsync<StringMessageTestingOnlyResponse>("/testingonly/negotiations/get?format=json");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGetWithFormatForXml_ThenReturnsXmlResponse()
    {
        var result = await Api.GetAsync("/testingonly/negotiations/get?format=xml");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                                   "<StringMessageTestingOnlyResponse xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
                                   "<Message>amessage</Message>" + "</StringMessageTestingOnlyResponse>");
    }
}
#endif