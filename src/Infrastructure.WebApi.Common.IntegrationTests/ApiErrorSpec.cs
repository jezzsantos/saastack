#if TESTINGONLY
using System.Net;
using ApiHost1;
using FluentAssertions;
using Infrastructure.WebApi.Interfaces.Operations.TestingOnly;
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
        var result = await Api.GetAsync(new ErrorsErrorTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task WhenGetThrowsException_ThenReturnsServerError()
    {
        var result = await Api.GetAsync(new ErrorsThrowTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        result.Content.Error.Type.Should().Be("https://tools.ietf.org/html/rfc7231#section-6.6.1");
        result.Content.Error.Title.Should().Be("An unexpected error occurred");
        result.Content.Error.Status.Should().Be(500);
        result.Content.Error.Detail.Should().Be("amessage");
        result.Content.Error.Instance.Should().Be("http://localhost/testingonly/errors/throws");
        result.Content.Error.Exception.Should().StartWith("System.InvalidOperationException: amessage");
        result.Content.Error.Errors.Should().BeNull();
        
    }
}
#endif