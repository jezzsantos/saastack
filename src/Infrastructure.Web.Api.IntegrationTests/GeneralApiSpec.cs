#if TESTINGONLY
using System.Net;
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
}
#endif