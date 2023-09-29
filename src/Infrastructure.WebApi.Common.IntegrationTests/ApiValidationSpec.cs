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
public class ApiValidationSpec : WebApiSpec<Program>
{
    public ApiValidationSpec(WebApiSetup<Program> setup) : base(setup)
    {
    }

    [Fact]
    public async Task WhenGetUnvalidatedRequest_ThenReturns200()
    {
        var result = await Api.GetAsync("/testingonly/validations/unvalidated");

        var json = await result.Content.ReadFromJsonAsync<StringMessageTestingOnlyResponse>();

        result.StatusCode.Should().Be(HttpStatusCode.OK);

        json?.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGetValidatedRequestWithInvalidFields_ThenReturnsValidationError()
    {
        var result = await Api.GetAsync("/testingonly/validations/validated/1234");

        var json = await result.Content.ReadAsStringAsync();

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        json.Should().Be("{" + "\"type\":\"NotEmptyValidator\"," + "\"title\":\"Validation Error\"," +
                         "\"status\":400," + "\"detail\":\"'Field1' must not be empty.\"," +
                         "\"instance\":\"http://localhost/testingonly/validations/validated/1234\"," + "\"errors\":[" +
                         "{\"rule\":\"NotEmptyValidator\",\"reason\":\"'Field1' must not be empty.\"}," +
                         "{\"rule\":\"NotEmptyValidator\",\"reason\":\"'Field2' must not be empty.\"}]}");
    }

    [Fact]
    public async Task WhenGetValidatedRequestWithValidId_ThenReturnsResponse()
    {
        var result = await Api.GetAsync("/testingonly/validations/validated/1234?Field1=123&Field2=456");

        var json = await result.Content.ReadFromJsonAsync<StringMessageTestingOnlyResponse>();

        result.StatusCode.Should().Be(HttpStatusCode.OK);

        json?.Message.Should().Be("amessage123");
    }
}
#endif