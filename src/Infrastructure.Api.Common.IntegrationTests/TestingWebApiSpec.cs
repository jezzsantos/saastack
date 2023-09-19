#if TESTINGONLY
using System.Net;
using System.Net.Http.Json;
using ApiHost1;
using FluentAssertions;
using Infrastructure.WebApi.Common;
using Infrastructure.WebApi.Interfaces.Operations.TestingOnly;
using IntegrationTesting.WebApi.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Infrastructure.Api.Common.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class TestingWebApiSpec : WebApiSpecSetup<Program>
{
    public TestingWebApiSpec(WebApplicationFactory<Program> factory) : base(factory)
    {
    }

    [Fact]
    public async Task WhenGetUnvalidatedRequest_ThenReturns200()
    {
        var result = await Api.GetAsync("/testingonly/1/unvalidated");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WhenGetUnvalidatedRequest_ThenReturnsJsonByDefault()
    {
        var result = await Api.GetFromJsonAsync<StringMessageTestingOnlyResponse>("/testingonly/1/unvalidated");

        result?.Message.Should().Be("amessage1");
    }

    [Fact]
    public async Task WhenGetValidatedRequestWithInvalidId_ThenReturnsValidationError()
    {
        var result = await Api.GetAsync("/testingonly/1234/validated");

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var json = await result.Content.ReadAsStringAsync();

        json.Should()
            .Be(
                "{" +
                "\"type\":\"NotEmptyValidator\"," +
                "\"title\":\"Validation Error\"," +
                "\"status\":400," +
                "\"detail\":\"'Field1' must not be empty.\"," +
                "\"instance\":\"http://localhost/testingonly/1234/validated\"," +
                "\"errors\":[" +
                "{\"rule\":\"NotEmptyValidator\",\"reason\":\"'Field1' must not be empty.\",\"value\":null}," +
                "{\"rule\":\"NotEmptyValidator\",\"reason\":\"'Field2' must not be empty.\",\"value\":null}]}");
    }

    [Fact]
    public async Task WhenGetThrowsException_ThenReturnsServerError()
    {
        var result = await Api.GetAsync("/testingonly/throws");

        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var json = await result.Content.ReadAsStringAsync();

        json.Should()
            .StartWith("{\"" +
                       "type\":\"https://tools.ietf.org/html/rfc7231#section-6.6.1\"," +
                       "\"title\":\"An unexpected error occurred\"," +
                       "\"status\":500," +
                       "\"detail\":\"amessage\"," +
                       "\"instance\":\"http://localhost/testingonly/throws\"," +
                       "\"exception\":\"System.InvalidOperationException: amessage");
    }


    [Fact]
    public async Task WhenGetWithNoAcceptAndNoFormat_ThenReturnsJsonResponse()
    {
        var result = await Api.GetAsync("/testingonly/1/unvalidated");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Content.ReadAsStringAsync();

        content.Should().Be("{\"message\":\"amessage1\"}");
    }

    [Fact]
    public async Task WhenGetWithAcceptForJson_ThenReturnsJsonResponse()
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri($"{Api.BaseAddress}testingonly/1/unvalidated")
        };
        request.Headers.Add(HttpHeaders.Accept, HttpContentTypes.Json);

        var result = await Api.SendAsync(request);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Content.ReadAsStringAsync();

        content.Should().Be("{\"message\":\"amessage1\"}");
    }

    [Fact]
    public async Task WhenGetWithAcceptForXml_ThenReturnsXmlResponse()
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri($"{Api.BaseAddress}testingonly/1/unvalidated")
        };
        request.Headers.Add(HttpHeaders.Accept, HttpContentTypes.Xml);

        var result = await Api.SendAsync(request);

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Content.ReadAsStringAsync();

        content.Should().Be($"<?xml version=\"1.0\" encoding=\"utf-8\"?>{Environment.NewLine}" +
                            $"<StringMessageTestingOnlyResponse xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">{Environment.NewLine}" +
                            $"  <Message>amessage1</Message>{Environment.NewLine}" +
                            "</StringMessageTestingOnlyResponse>");
    }

    [Fact]
    public async Task WhenGetWithAcceptForUnsupported_ThenReturns415()
    {
        var request = new HttpRequestMessage
        {
            RequestUri = new Uri($"{Api.BaseAddress}testingonly/1/unvalidated")
        };
        request.Headers.Add(HttpHeaders.Accept, "application/unsupported");

        var result = await Api.SendAsync(request);

        result.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        var content = await result.Content.ReadAsStringAsync();

        content.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenGetWithFormatForJson_ThenReturnsJsonResponse()
    {
        var result = await Api.GetAsync("/testingonly/1/unvalidated?format=json");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Content.ReadAsStringAsync();

        content.Should().Be("{\"message\":\"amessage1\"}");
    }

    [Fact]
    public async Task WhenGetWithFormatForXml_ThenReturnsXmlResponse()
    {
        var result = await Api.GetAsync("/testingonly/1/unvalidated?format=xml");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Content.ReadAsStringAsync();

        content.Should().Be($"<?xml version=\"1.0\" encoding=\"utf-8\"?>{Environment.NewLine}" +
                            $"<StringMessageTestingOnlyResponse xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">{Environment.NewLine}" +
                            $"  <Message>amessage1</Message>{Environment.NewLine}" +
                            "</StringMessageTestingOnlyResponse>");
    }

    [Fact]
    public async Task WhenGetWithFormatForUnsupported_ThenReturns415()
    {
        var result = await Api.GetAsync("/testingonly/1/unvalidated?format=unsupported");

        result.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        var content = await result.Content.ReadAsStringAsync();

        content.Should().BeEmpty();
    }
}
#endif