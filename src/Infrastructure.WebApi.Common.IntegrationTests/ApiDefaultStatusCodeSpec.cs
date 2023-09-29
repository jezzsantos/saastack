#if TESTINGONLY
using System.Net;
using ApiHost1;
using FluentAssertions;
using Infrastructure.WebApi.Interfaces.Operations.TestingOnly;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace Infrastructure.WebApi.Common.IntegrationTests;

[Trait("Category", "Integration.Web")]
public class ApiDefaultStatusCode : WebApiSpec<Program>
{
    public ApiDefaultStatusCode(WebApiSetup<Program> setup) : base(setup)
    {
    }

    [Fact]
    public async Task WhenPost_ThenReturnsCreated()
    {
        var result = await Api.PostAsync("/testingonly/statuses/post", new StatusesPostTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Headers.Location.Should().Be("alocation");
        result.Content.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenPostWithoutLocation_ThenReturnsOk()
    {
        var result = await Api.PostAsync("/testingonly/statuses/post2",
            new StatusesPostWithLocationTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Headers.Location.Should().BeNull();
        result.Content.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGet_ThenReturnsOk()
    {
        var result = await Api.GetAsync<StatusesTestingOnlyResponse>("/testingonly/statuses/get");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenSearch_ThenReturnsOk()
    {
        var result = await Api.GetAsync<StatusesTestingOnlyResponse>("/testingonly/statuses/search");

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenPut_ThenReturnsAccepted()
    {
        var result = await Api.PutAsync("/testingonly/statuses/putpatch", new StatusesPutPatchTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.Accepted);
        result.Content.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenPatch_ThenReturnsAccepted()
    {
        var result = await Api.PatchAsync("/testingonly/statuses/putpatch", new StatusesPutPatchTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.Accepted);
        result.Content.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenDelete_ThenReturnsNoContent()
    {
        var result = await Api.DeleteAsync("/testingonly/statuses/delete");

        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        result.Content.Should().BeEmpty();
    }
}
#endif