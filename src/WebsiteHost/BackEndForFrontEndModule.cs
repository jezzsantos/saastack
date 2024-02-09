using System.Reflection;
using System.Text.Json;
using Application.Interfaces.Services;
using Domain.Services.Shared.DomainServices;
using Infrastructure.Common.DomainServices;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Web.Common.Clients;
using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Hosting.Common.Pipeline;
using Infrastructure.Web.Interfaces.Clients;
using WebsiteHost.Api.Recording;
using WebsiteHost.Application;

namespace WebsiteHost;

public class BackEndForFrontEndModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof(RecordingApi).Assembly;

    public Assembly? DomainAssembly => null;

    public Dictionary<Type, string> AggregatePrefixes => new();

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
                services.RegisterUnshared<IRecordingApplication, RecordingApplication>();
                services.RegisterUnshared<IAuthenticationApplication, AuthenticationApplication>();
                services.RegisterUnshared<IServiceClient>(c =>
                    new InterHostServiceClient(c.Resolve<IHttpClientFactory>(),
                        c.Resolve<JsonSerializerOptions>(),
                        c.Resolve<IHostSettings>().GetApiHost1BaseUrl()));
                services.RegisterUnshared<IEncryptionService>(c => new AesEncryptionService(c
                    .ResolveForUnshared<IHostSettings>().GetWebsiteHostCSRFEncryptionSecret()));
                services.RegisterUnshared<CSRFMiddleware.ICSRFService, CSRFService>();
            };
        }
    }
}