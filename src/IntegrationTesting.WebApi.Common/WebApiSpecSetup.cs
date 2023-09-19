using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace IntegrationTesting.WebApi.Common;

public abstract class WebApiSpecSetup<THost> : IClassFixture<WebApplicationFactory<THost>> where THost : class
{
    protected readonly HttpClient Api;

    protected WebApiSpecSetup(WebApplicationFactory<THost> factory)
    {
        Api = factory
            .WithWebHostBuilder(builder => builder.ConfigureServices(_ =>
            {
                //TODO: swap out dependencies
                //services.AddScoped<ITodoItemService, TestTodoItemService>();
            }))
            .CreateClient();
    }
}