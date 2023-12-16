#if TESTINGONLY
using System.Net;
using ApiHost1;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace Infrastructure.Web.Api.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class ApiDefaultStatusCode : WebApiSpec<Program>
{
    public ApiDefaultStatusCode(WebApiSetup<Program> setup) : base(setup)
    {
    }

    [Fact]
    public async Task WhenPost_ThenReturnsCreated()
    {
        var result = await Api.PostAsync(new StatusesPostTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Headers.Location.Should().Be("alocation");
        result.Content.Value.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenPostWithoutLocation_ThenReturnsOk()
    {
        var result = await Api.PostAsync(new StatusesPostWithLocationTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Headers.Location.Should().BeNull();
        result.Content.Value.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGet_ThenReturnsOk()
    {
        var result = await Api.GetAsync(new StatusesGetTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenSearch_ThenReturnsOk()
    {
        var result = await Api.GetAsync(new StatusesSearchTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Messages.Should().ContainInOrder("amessage");
    }

    [Fact]
    public async Task WhenPut_ThenReturnsAccepted()
    {
        var result = await Api.PutAsync(new StatusesPutPatchTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.Accepted);
        result.Content.Value.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenPatch_ThenReturnsAccepted()
    {
        var result = await Api.PatchAsync(new StatusesPutPatchTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.Accepted);
        result.Content.Value.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenDelete_ThenReturnsNoContent()
    {
        var result = await Api.DeleteAsync(new StatusesDeleteTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        result.Content.HasValue.Should().BeFalse();
    }
}
#endif