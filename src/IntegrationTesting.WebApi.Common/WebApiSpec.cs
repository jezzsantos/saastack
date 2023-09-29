using Infrastructure.WebApi.Common.Clients;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace IntegrationTesting.WebApi.Common;

[UsedImplicitly]
public class WebApiSetup<THost> : WebApplicationFactory<THost>
    where THost : class
{
    private IServiceScope? _scope;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _scope?.Dispose();
        }

        base.Dispose(disposing);
    }

    public TInterface GetRequiredService<TInterface>()
        where TInterface : notnull
    {
        if (_scope is null)
        {
            _scope = Services.GetRequiredService<IServiceScopeFactory>().CreateScope();
        }

        return _scope.ServiceProvider.GetRequiredService<TInterface>();
    }
}

public abstract class WebApiSpec<THost> : IClassFixture<WebApiSetup<THost>>, IDisposable
    where THost : class
{
    private readonly WebApplicationFactory<THost> _setup;
    protected readonly IHttpJsonClient Api;
    protected readonly HttpClient HttpApi;

    protected WebApiSpec(WebApiSetup<THost> setup)
    {
        _setup = setup.WithWebHostBuilder(builder => builder.ConfigureServices(_ =>
        {
            //TODO: swap out dependencies
            //services.AddScoped<ITodoItemService, TestTodoItemService>();
        }));
        HttpApi = setup.CreateClient();
        Api = new JsonClient(HttpApi);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            HttpApi.Dispose();
            _setup.Dispose();
        }
    }
}