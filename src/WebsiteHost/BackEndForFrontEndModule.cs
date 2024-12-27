using System.Reflection;
using System.Text.Json;
using Application.Interfaces.Services;
using Domain.Services.Shared;
using Infrastructure.Common.DomainServices;
using Infrastructure.Web.Api.Common.Clients;
using Infrastructure.Web.Api.Interfaces.Clients;
using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Hosting.Common.Pipeline;
using WebsiteHost.Api.Recording;
using WebsiteHost.Application;
using WebsiteHost.ApplicationServices;

namespace WebsiteHost;

public class BackEndForFrontEndModule : ISubdomainModule
{
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
                    webApp.MapControllerRoute("index", "index.html", new { controller = "Home", action = "Index" });
                    webApp.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
                    webApp.MapFallbackToController("Index", "Home"); // To support SPA applications
                }, "Pipeline: MVC controllers and views are enabled"));
            };
        }
    }

    public Assembly? DomainAssembly => null;

    public Dictionary<Type, string> EntityPrefixes => new();

    public Assembly InfrastructureAssembly => typeof(RecordingApi).Assembly;

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                services.AddControllersWithViews();
                services.AddSingleton<IWebPackBundler, WebPackBundler>();
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