#if TESTINGONLY
using System.Net;
using System.Net.Http.Json;
using System.Text;
using ApiHost1;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.WebApi.Interfaces.Operations.TestingOnly;
using IntegrationTesting.WebApi.Common;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Infrastructure.WebApi.Common.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class TestingWebApiSpec : WebApiSpecSetup<Program>
{
    public TestingWebApiSpec(WebApplicationFactory<Program> factory) : base(factory)
    {
    }


    [Fact]
    public async Task WhenGetError_ThenReturnsError()
    {
        var result = await Api.GetAsync("/testingonly/error");

        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
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
                "{\"rule\":\"NotEmptyValidator\",\"reason\":\"'Field1' must not be empty.\"}," +
                "{\"rule\":\"NotEmptyValidator\",\"reason\":\"'Field2' must not be empty.\"}]}");
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

        content.Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                            "<StringMessageTestingOnlyResponse xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
                            "<Message>amessage1</Message>" +
                            "</StringMessageTestingOnlyResponse>");
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
    public async Task WhenGetWithFormatForUnsupported_ThenReturns415()
    {
        var result = await Api.GetAsync("/testingonly/1/unvalidated?format=unsupported");

        result.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
        var content = await result.Content.ReadAsStringAsync();

        content.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenGetWithFormatForXml_ThenReturnsXmlResponse()
    {
        var result = await Api.GetAsync("/testingonly/1/unvalidated?format=xml");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await result.Content.ReadAsStringAsync();

        content.Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                            "<StringMessageTestingOnlyResponse xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
                            "<Message>amessage1</Message>" +
                            "</StringMessageTestingOnlyResponse>");
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
    public async Task WhenPostWithDifferentDataTypesForJson_ThenReturnsValues()
    {
        var time1 = DateTime.UtcNow;
        var time2 = time1.AddHours(1);
        var request = new
        {
            Time = time1.ToIso8601(),
            Integer = 9,
            Double = 99.9,
            String = "avalue1",
            Enum = $"{CustomEnum.OneHundredAndOne}",
            Custom = new
            {
                Time = time2.ToIso8601(),
                Integer = 91,
                Double = 91.1,
                String = "avalue2",
                Enum = $"{CustomEnum.TwentyOne}"
            }
        }.ToJson()!;

        var result = await Api.PostAsync("/testingonly/roundtrip",
            new StringContent(request, Encoding.UTF8, HttpContentTypes.Json));

        var response = await result.Content.ReadAsStringAsync();

        response.Should().Be("{" +
                             "\"custom\":" +
                             "{" +
                             "\"double\":91.1," +
                             "\"enum\":\"twentyOne\"," +
                             "\"integer\":91," +
                             "\"string\":\"avalue2\"," +
                             $"\"time\":\"{time2.ToIso8601()}\"" +
                             "}," +
                             "\"double\":99.9," +
                             "\"enum\":\"oneHundredAndOne\"," +
                             "\"integer\":9," +
                             "\"string\":\"avalue1\"," +
                             $"\"time\":\"{time1.ToIso8601()}\"" +
                             "}");
    }

    [Fact]
    public async Task WhenPostWithDifferentDataTypesForXml_ThenReturnsValues()
    {
        var time1 = DateTime.UtcNow;
        var time2 = time1.AddHours(1);
        var request = new
        {
            Time = time1.ToIso8601(),
            Integer = 9,
            Double = 99.9,
            String = "avalue1",
            Enum = $"{CustomEnum.OneHundredAndOne}",
            Custom = new
            {
                Time = time2.ToIso8601(),
                Integer = 91,
                Double = 91.1,
                String = "avalue2",
                Enum = $"{CustomEnum.TwentyOne}"
            }
        }.ToJson()!;

        var result = await Api.PostAsync("/testingonly/roundtrip?format=xml",
            new StringContent(request, Encoding.UTF8, HttpContentTypes.Json));

        var response = await result.Content.ReadAsStringAsync();

        response.Should()
            .Be("<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<DataTypesTestingOnlyResponse xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">" +
                "<Custom>" +
                "<Double>91.1</Double>" +
                "<Enum>TwentyOne</Enum>" +
                "<Integer>91</Integer>" +
                "<String>avalue2</String>" +
                $"<Time>{time2.ToIso8601()}</Time>" +
                "</Custom><Double>99.9</Double>" +
                "<Enum>OneHundredAndOne</Enum>" +
                "<Integer>9</Integer>" +
                "<String>avalue1</String>" +
                $"<Time>{time1.ToIso8601()}</Time>" +
                "</DataTypesTestingOnlyResponse>");
    }

    [Fact]
    public async Task WhenPostWithIso8601DateTime_ThenReturnsUnixTimestamp()
    {
        var time1 = DateTime.UtcNow;
        var time2 = time1.AddHours(1);
        var request = new
        {
            Time = time1.ToIso8601(),
            Custom = new
            {
                Time = time2.ToIso8601()
            }
        }.ToJson()!;

        var result = await Api.PostAsync("/testingonly/roundtrip",
            new StringContent(request, Encoding.UTF8, HttpContentTypes.Json));

        var response = await result.Content.ReadAsStringAsync();

        response.Should().Be("{" +
                             "\"custom\":" +
                             "{" +
                             $"\"time\":\"{time2.ToIso8601()}\"" +
                             "}," +
                             $"\"time\":\"{time1.ToIso8601()}\"" +
                             "}");
    }

    [Fact]
    public async Task WhenPostWithUnixSecondsDateTime_ThenReturnsUnixTimestamp()
    {
        var time1 = DateTime.UtcNow.ToNearestSecond();
        var time2 = time1.AddHours(1);
        var request = new
        {
            Time = time1.ToUnixSeconds(),
            Custom = new
            {
                Time = time2.ToUnixSeconds()
            }
        }.ToJson()!;

        var result = await Api.PostAsync("/testingonly/roundtrip",
            new StringContent(request, Encoding.UTF8, HttpContentTypes.Json));

        var response = await result.Content.ReadAsStringAsync();

        response.Should().Be("{" +
                             "\"custom\":" +
                             "{" +
                             $"\"time\":\"{time2.ToIso8601()}\"" +
                             "}," +
                             $"\"time\":\"{time1.ToIso8601()}\"" +
                             "}");
    }

    [Fact]
    public async Task WhenPostWithLowercaseEnum_ThenReturnsCamelcased()
    {
        var request = new
        {
            Enum = $"{CustomEnum.OneHundredAndOne.ToString().ToLowerInvariant()}",
            Custom = new
            {
                Enum = $"{CustomEnum.TwentyOne.ToString().ToLowerInvariant()}"
            }
        }.ToJson()!;

        var result = await Api.PostAsync("/testingonly/roundtrip",
            new StringContent(request, Encoding.UTF8, HttpContentTypes.Json));

        var response = await result.Content.ReadAsStringAsync();

        response.Should().Be("{" +
                             "\"custom\":" +
                             "{" +
                             "\"enum\":\"twentyOne\"" +
                             "}," +
                             "\"enum\":\"oneHundredAndOne\"" +
                             "}");
    }
}
#endif