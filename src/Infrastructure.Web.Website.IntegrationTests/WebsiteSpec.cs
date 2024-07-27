using System.Text.Json;
using ApiHost1;
using Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Hosting.Common.Pipeline;
using Infrastructure.Web.Website.IntegrationTests.Stubs;
using IntegrationTesting.WebApi.Common;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Web.Website.IntegrationTests;

public abstract class WebsiteSpec<THost> : WebApiSpec<THost>
    where THost : class
{
    protected readonly CSRFMiddleware.ICSRFService CSRFService;
    protected readonly JsonSerializerOptions JsonOptions;

    protected WebsiteSpec(WebApiSetup<THost> setup, Action<IServiceCollection>? overrideDependencies = null,
        Action<WebApiSpec<THost>>? runOnceBeforeAllTests = null,
        Action<WebApiSpec<THost>>? runOnceAfterAllTests = null) : base(
        setup, OverrideDependencies(overrideDependencies), spec =>
        {
            spec.StartupAdditionalServer<Program>();
            runOnceBeforeAllTests?.Invoke(spec);
        }, spec =>
        {
            runOnceAfterAllTests?.Invoke(spec);
            spec.ShutdownAllAdditionalServers();
        })
    {
        CSRFService = setup.GetRequiredService<CSRFMiddleware.ICSRFService>();
#if TESTINGONLY
        HttpApi.PostEmptyJsonAsync(new DestroyAllRepositoriesRequest().MakeApiRoute(),
                (msg, cookies) => msg.WithCSRF(cookies, CSRFService)).GetAwaiter()
            .GetResult();
#endif
        JsonOptions = setup.GetRequiredService<JsonSerializerOptions>();
    }

    private static Action<IServiceCollection> OverrideDependencies(Action<IServiceCollection>? overrideDependencies)
    {
        if (overrideDependencies.Exists())
        {
            return services =>
            {
                services.AddSingleton<IHttpClientFactory, StubHttpClientFactory>();
                overrideDependencies(services);
            };
        }

        return services => services.AddSingleton<IHttpClientFactory, StubHttpClientFactory>();
    }
}