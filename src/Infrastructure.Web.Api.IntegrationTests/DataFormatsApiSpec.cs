#if TESTINGONLY
using System.Text;
using ApiHost1;
using Common.Extensions;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using IntegrationTesting.WebApi.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Web.Api.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class DataFormatsApiSpec : WebApiSpec<Program>
{
    public DataFormatsApiSpec(WebApiSetup<Program> setup) : base(setup)
    {
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

        var result = await HttpApi.PostAsync("/testingonly/formats/roundtrip",
            new StringContent(request, Encoding.UTF8, HttpConstants.ContentTypes.Json));

        var content = await result.Content.ReadAsStringAsync();
        content.Should().Be("{" + "\"custom\":" + "{" + "\"double\":91.1," + "\"enum\":\"twentyOne\","
                            + "\"integer\":91,"
                            + "\"string\":\"avalue2\"," + $"\"time\":\"{time2.ToIso8601()}\"" + "},"
                            + "\"double\":99.9,"
                            + "\"enum\":\"oneHundredAndOne\"," + "\"integer\":9," + "\"string\":\"avalue1\","
                            + $"\"time\":\"{time1.ToIso8601()}\"" + "}");
    }

    [Fact]
    public async Task WhenPostWithDifferentDataTypesForXml_ThenReturnsValues()
    {
        var time1 = DateTime.UtcNow.ToNearestSecond();
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

        var result = await HttpApi.PostAsync("/testingonly/formats/roundtrip?format=xml",
            new StringContent(request, Encoding.UTF8, HttpConstants.ContentTypes.Json));

        var content = await result.Content.ReadAsStringAsync();
        content.Should().Be("<?xml version=\"1.0\" encoding=\"utf-8\"?>"
                            + "<FormatsTestingOnlyResponse xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">"
                            + "<Custom>" + "<Double>91.1</Double>" + "<Enum>TwentyOne</Enum>"
                            + "<Integer>91</Integer>"
                            + "<String>avalue2</String>" + $"<Time>{time2:yyyy-MM-ddTHH:mm:ssZ}</Time>"
                            + "</Custom><Double>99.9</Double>"
                            + "<Enum>OneHundredAndOne</Enum>" + "<Integer>9</Integer>"
                            + "<String>avalue1</String>"
                            + $"<Time>{time1:yyyy-MM-ddTHH:mm:ssZ}</Time>" + "</FormatsTestingOnlyResponse>");
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

        var result = await HttpApi.PostAsync("/testingonly/formats/roundtrip",
            new StringContent(request, Encoding.UTF8, HttpConstants.ContentTypes.Json));

        var content = await result.Content.ReadAsStringAsync();
        content.Should().Be("{" + "\"custom\":" + "{" + $"\"time\":\"{time2.ToIso8601()}\"" + "},"
                            + $"\"time\":\"{time1.ToIso8601()}\"" + "}");
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

        var result = await HttpApi.PostAsync("/testingonly/formats/roundtrip",
            new StringContent(request, Encoding.UTF8, HttpConstants.ContentTypes.Json));

        var content = await result.Content.ReadAsStringAsync();
        content.Should().Be("{" + "\"custom\":" + "{" + $"\"time\":\"{time2.ToIso8601()}\"" + "},"
                            + $"\"time\":\"{time1.ToIso8601()}\"" + "}");
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

        var result = await HttpApi.PostAsync("/testingonly/formats/roundtrip",
            new StringContent(request, Encoding.UTF8, HttpConstants.ContentTypes.Json));

        var content = await result.Content.ReadAsStringAsync();
        content.Should().Be("{" + "\"custom\":" + "{" + "\"enum\":\"twentyOne\"" + "},"
                            + "\"enum\":\"oneHundredAndOne\"" + "}");
    }
}
#endif