#if TESTINGONLY
using System.Net;
using System.Net.Http.Json;
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
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri($"{Api.BaseAddress}testingonly/negotiations/get")
        };
        request.Headers.Remove(HttpHeaders.Accept);

        var result = await Api.SendAsync(request);

        var json = await result.Content.ReadFromJsonAsync<StringMessageTestingOnlyResponse>();

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        json?.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGetWithAcceptForUnsupported_ThenReturns415()
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri($"{Api.BaseAddress}testingonly/negotiations/get")
        };
        request.Headers.Add(HttpHeaders.Accept, "application/unsupported");

        var result = await Api.SendAsync(request);

        var content = await result.Content.ReadAsStringAsync();

        result.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenGetWithAcceptForJson_ThenReturnsJsonResponse()
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri($"{Api.BaseAddress}testingonly/negotiations/get")
        };
        request.Headers.Add(HttpHeaders.Accept, HttpContentTypes.Json);

        var result = await Api.SendAsync(request);

        var json = await result.Content.ReadFromJsonAsync<StringMessageTestingOnlyResponse>();

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        json?.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGetWithAcceptForXml_ThenReturnsXmlResponse()
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri($"{Api.BaseAddress}testingonly/negotiations/get")
        };
        request.Headers.Add(HttpHeaders.Accept, HttpContentTypes.Xml);

        var result = await Api.SendAsync(request);

        var content = await result.Content.ReadAsStringAsync();

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                            "<StringMessageTestingOnlyResponse xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
                            "<Message>amessage</Message>" + "</StringMessageTestingOnlyResponse>");
    }

    [Fact]
    public async Task WhenGetWithNoFormat_ThenReturnsJsonResponse()
    {
        var result = await Api.GetAsync("/testingonly/negotiations/get");

        var json = await result.Content.ReadFromJsonAsync<StringMessageTestingOnlyResponse>();

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        json?.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGetWithFormatForUnsupported_ThenReturns415()
    {
        var result = await Api.GetAsync("/testingonly/negotiations/get?format=unsupported");

        var content = await result.Content.ReadAsStringAsync();

        result.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        content.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenGetWithFormatForJson_ThenReturnsJsonResponse()
    {
        var result = await Api.GetAsync("/testingonly/negotiations/get?format=json");

        var json = await result.Content.ReadFromJsonAsync<StringMessageTestingOnlyResponse>();

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        json?.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGetWithFormatForXml_ThenReturnsXmlResponse()
    {
        var result = await Api.GetAsync("/testingonly/negotiations/get?format=xml");

        var content = await result.Content.ReadAsStringAsync();

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        content.Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                            "<StringMessageTestingOnlyResponse xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
                            "<Message>amessage</Message>" + "</StringMessageTestingOnlyResponse>");
    }
}
#endif