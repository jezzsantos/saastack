#if TESTINGONLY
using System.Net;
using ApiHost1;
using FluentAssertions;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace Infrastructure.WebApi.Common.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class ApiErrorSpec : WebApiSpec<Program>
{
    public ApiErrorSpec(WebApiSetup<Program> setup) : base(setup)
    {
    }

    [Fact]
    public async Task WhenGetError_ThenReturnsError()
    {
        var result = await Api.GetAsync("/testingonly/errors/error");

        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task WhenGetThrowsException_ThenReturnsServerError()
    {
        var result = await Api.GetAsync("/testingonly/errors/throws");

        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        var json = await result.Content.ReadAsStringAsync();

        json.Should().StartWith("{\"" + "type\":\"https://tools.ietf.org/html/rfc7231#section-6.6.1\"," +
                                "\"title\":\"An unexpected error occurred\"," + "\"status\":500," +
                                "\"detail\":\"amessage\"," +
                                "\"instance\":\"http://localhost/testingonly/errors/throws\"," +
                                "\"exception\":\"System.InvalidOperationException: amessage");
    }
}
#endif