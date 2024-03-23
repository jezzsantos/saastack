using System.Reflection;
using System.Text.Json;
using Application.Interfaces.Services;
using Domain.Services.Shared.DomainServices;
using Infrastructure.Common.DomainServices;
using Infrastructure.Web.Common.Clients;
using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Hosting.Common.Pipeline;
using Infrastructure.Web.Interfaces.Clients;
using WebsiteHost.Api.Recording;
using WebsiteHost.Application;

namespace WebsiteHost;

public class BackEndForFrontEndModule : ISubdomainModule
{
    public Assembly InfrastructureAssembly => typeof(RecordingApi).Assembly;

    public Assembly? DomainAssembly => null;

    public Dictionary<Type, string> EntityPrefixes => new();

    public Action<WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware
    {
        get
        {
            return (app, middlewares) =>
            {
                app.RegisterRoutes();
                middlewares.Add(new MiddlewareRegistration(33, webApp =>
                {
                    if (!webApp.Environment.IsDevelopment())
                    {
                        webApp.UseExceptionHandler("/Home/Error");
                    }

                    webApp.UseRouting();
                    webApp.MapControllerRoute(
                        "default",
                        "{controller=Home}/{action=Index}/{id?}");
                }, "Pipeline: MVC controllers are enabled"));
            };
        }
    }

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                services.AddControllers();
                services.AddSingleton<IFeatureFlagsApplication, FeatureFlagsApplication>();
                services.AddSingleton<IRecordingApplication, RecordingApplication>();
                services.AddSingleton<IAuthenticationApplication, AuthenticationApplication>();
                services.AddSingleton<IServiceClient>(c =>
                    new InterHostServiceClient(c.GetRequiredService<IHttpClientFactory>(),
                        c.GetRequiredService<JsonSerializerOptions>(),
                        c.GetRequiredService<IHostSettings>().GetApiHost1BaseUrl()));
                services.AddSingleton<IEncryptionService>(c =>
                    new AesEncryptionService(c.GetRequiredService<IHostSettings>()
                        .GetWebsiteHostCSRFEncryptionSecret()));
                services.AddSingleton<CSRFMiddleware.ICSRFService, CSRFService>();
            };
        }
    }
}