#if TESTINGONLY
using System.Net;
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
        var result = await Api.GetAsync(new ValidationsUnvalidatedTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGetValidatedRequestWithInvalidFields_ThenReturnsValidationError()
    {
        var result = await Api.GetAsync(new ValidationsValidatedTestingOnlyRequest { Id = "1234" });

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Content.Error.Type.Should().Be("NotEmptyValidator");
        result.Content.Error.Title.Should().Be("Validation Error");
        result.Content.Error.Status.Should().Be(400);
        result.Content.Error.Detail.Should().Be("'Field1' must not be empty.");
        result.Content.Error.Instance.Should().Be("http://localhost/testingonly/validations/validated/1234");
        result.Content.Error.Exception.Should().BeNull();
        result.Content.Error.Errors.Should().BeEquivalentTo(new ValidatorProblem[]
        {
            new() { Rule = "NotEmptyValidator", Reason = "'Field1' must not be empty.", Value = null },
            new() { Rule = "NotEmptyValidator", Reason = "'Field2' must not be empty.", Value = null }
        });
    }

    [Fact]
    public async Task WhenGetValidatedRequestWithPartialInvalidFields_ThenReturnsValidationError()
    {
        var result = await Api.GetAsync(new ValidationsValidatedTestingOnlyRequest { Id = "1234", Field1 = "123" });

        result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        result.Content.Error.Type.Should().Be("NotEmptyValidator");
        result.Content.Error.Title.Should().Be("Validation Error");
        result.Content.Error.Status.Should().Be(400);
        result.Content.Error.Detail.Should().Be("'Field2' must not be empty.");
        result.Content.Error.Instance.Should().Be("http://localhost/testingonly/validations/validated/1234?field1=123");
        result.Content.Error.Exception.Should().BeNull();
        result.Content.Error.Errors.Should().BeEquivalentTo(new ValidatorProblem[]
        {
            new() { Rule = "NotEmptyValidator", Reason = "'Field2' must not be empty.", Value = null }
        });
    }
    [Fact]
    public async Task WhenGetValidatedRequestWithValidId_ThenReturnsResponse()
    {
        var result =
            await Api.GetAsync(new ValidationsValidatedTestingOnlyRequest
                { Id = "1234", Field1 = "123", Field2 = "456" });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Message.Should().Be("amessage123");
    }
}
#endif