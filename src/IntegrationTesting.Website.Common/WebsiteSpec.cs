using System.Text.Json;
using Common.Extensions;
using Infrastructure.Web.Api.Operations.Shared.TestingOnly;
using Infrastructure.Web.Hosting.Common.Pipeline;
using IntegrationTesting.WebApi.Common;
using IntegrationTesting.Website.Common.Stubs;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationTesting.Website.Common;

public abstract class WebsiteSpec<TWebsiteHost, TApiHost> : WebApiSpec<TWebsiteHost>
    where TWebsiteHost : class
    where TApiHost : class
{
    protected readonly CSRFMiddleware.ICSRFService CSRFService;
    protected readonly JsonSerializerOptions JsonOptions;

    protected WebsiteSpec(WebApiSetup<TWebsiteHost> setup, Action<IServiceCollection>? overrideDependencies = null,
        Action<WebApiSpec<TWebsiteHost>>? runOnceBeforeAllTests = null,
        Action<WebApiSpec<TWebsiteHost>>? runOnceAfterAllTests = null) : base(
        setup, OverrideDependencies(overrideDependencies), spec =>
        {
            spec.StartupAdditionalServer<TApiHost>();
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