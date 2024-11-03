#if TESTINGONLY
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using IntegrationTesting.WebApi.Common;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Web.Api.IntegrationTests;

[UsedImplicitly]
[Trait("Category", "Integration.API")]
[Collection("API")]
public class GeneralApiSpec : WebApiSpec<ApiHost1.Program>
{
    public GeneralApiSpec(WebApiSetup<ApiHost1.Program> setup) : base(setup)
    {
    }

    [Fact]
    public async Task WhenPostWithEnum_ThenReturns()
    {
        var result = await Api.PostAsync(new PostWithEnumTestingOnlyRequest
        {
            AnEnum = TestEnum.Value1,
            AProperty = null
        });

        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Content.Value.Message.Should().Be("amessageValue1");
    }

    [Fact]
    public async Task WhenPostWithEmptyBody_ThenReturns()
    {
        var result = await Api.PostAsync(new PostWithEmptyBodyTestingOnlyRequest());

        result.StatusCode.Should().Be(HttpStatusCode.Created);
        result.Content.Value.Message.Should().Be("amessage");
    }

    [Fact]
    public async Task WhenPostWithRouteParamsAndEmptyBody_ThenReturns()
    {
        var result = await HttpApi.PostAsync("/testingonly/general/body/avalue/99/route",
            JsonContent.Create(new PostWithRouteParamsAndEmptyBodyTestingOnlyRequest()));

        result.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await result.Content.ReadAsStringAsync();
        content.Should().Be("{\"message\":\"amessageavalue99\"}");
    }

    [Fact]
    public async Task WhenGetWithSimpleArray_ThenReturns()
    {
        var result = await Api.GetAsync(new GetWithSimpleArrayTestingOnlyRequest
        {
            AnArray = new[] { "a", "b", "c" }
        });

        result.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Content.Value.Message.Should().Be("a, b, c");
    }
}
#endif