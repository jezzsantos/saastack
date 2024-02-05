using System.Reflection;
using System.Text.Json;
using Application.Interfaces.Services;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Web.Common.Clients;
using Infrastructure.Web.Hosting.Common;
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
        get { return (app, _) => app.RegisterRoutes(); }
    }

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                services.RegisterUnshared<IRecordingApplication, RecordingApplication>();
                services.RegisterUnshared<IAuthenticationApplication, AuthenticationApplication>();
                services.RegisterUnshared<IServiceClient>(c =>
                    new InterHostServiceClient(c.Resolve<IHttpClientFactory>(),
                        c.Resolve<JsonSerializerOptions>(),
                        c.Resolve<IHostSettings>().GetApiHost1BaseUrl()));
            };
        }
    }
}