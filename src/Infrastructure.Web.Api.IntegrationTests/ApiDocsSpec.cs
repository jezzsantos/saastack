using FluentAssertions;
using HtmlAgilityPack;
using Infrastructure.Hosting.Common;
using IntegrationTesting.WebApi.Common;
using Xunit;

namespace Infrastructure.Web.Api.IntegrationTests;

[Trait("Category", "Integration.API")]
[Collection("API")]
public class ApiDocsSpec : WebApiSpec<ApiHost1.Program>
{
    public ApiDocsSpec(WebApiSetup<ApiHost1.Program> setup) : base(setup)
    {
    }

    [Fact]
    public async Task WhenGetSwaggerUI_ThenDisplayed()
    {
        var result = await HttpApi.GetAsync("/index.html");

        var content = await result.Content.ReadAsStringAsync();
        content.Should().Contain("<html");

        var doc = new HtmlDocument();
        doc.Load(await result.Content.ReadAsStreamAsync());

        var swaggerPage = doc.DocumentNode
            .SelectSingleNode("//div[@id='swagger-ui']");

        swaggerPage.Should().NotBeNull();

        var title = doc.DocumentNode.SelectSingleNode("//title");

        title.Should().NotBeNull();
        title!.InnerText.Should().Be(HostOptions.BackEndAncillaryApiHost.HostName);
    }
}