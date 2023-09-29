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
public class ApiDefaultStatusCode : WebApiSpec<Program>
{
    public ApiDefaultStatusCode(WebApiSetup<Program> setup) : base(setup)
    {
    }

    [Fact]
    public async Task WhenPost_ThenReturnsCreated()
    {
        var result = await Api.PostAsJsonAsync("/testingonly/statuses/post", new StatusesPostTestingOnlyRequest());

        var json = await result.Content.ReadFromJsonAsync<StatusesTestingOnlyResponse>();

        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Headers.Location.Should().Be("alocation");
        json?.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenPostWithoutLocation_ThenReturnsOk()
    {
        var result = await Api.PostAsJsonAsync("/testingonly/statuses/post2",
            new StatusesPostWithLocationTestingOnlyRequest());

        var json = await result.Content.ReadFromJsonAsync<StatusesTestingOnlyResponse>();

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Headers.Location.Should().BeNull();
        json?.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenGet_ThenReturnsOk()
    {
        var result = await Api.GetAsync("/testingonly/statuses/get");

        var json = await result.Content.ReadFromJsonAsync<StatusesTestingOnlyResponse>();

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        json?.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenSearch_ThenReturnsOk()
    {
        var result = await Api.GetAsync("/testingonly/statuses/search");

        var json = await result.Content.ReadFromJsonAsync<StatusesTestingOnlyResponse>();

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        json?.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenPut_ThenReturnsAccepted()
    {
        var result =
            await Api.PutAsJsonAsync("/testingonly/statuses/putpatch", new StatusesPutPatchTestingOnlyRequest());

        var json = await result.Content.ReadFromJsonAsync<StatusesTestingOnlyResponse>();

        result.StatusCode.Should().Be(HttpStatusCode.Accepted);
        json?.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenPatch_ThenReturnsAccepted()
    {
        var result =
            await Api.PatchAsJsonAsync("/testingonly/statuses/putpatch", new StatusesPutPatchTestingOnlyRequest());

        var json = await result.Content.ReadFromJsonAsync<StatusesTestingOnlyResponse>();

        result.StatusCode.Should().Be(HttpStatusCode.Accepted);
        json?.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenDelete_ThenReturnsNoContent()
    {
        var result = await Api.DeleteAsync("/testingonly/statuses/delete");

        var json = await result.Content.ReadAsStringAsync();

        result.StatusCode.Should().Be(HttpStatusCode.NoContent);
        json.Should().BeEmpty();
    }
}
#endif