using System.Reflection;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Web.Hosting.Common;
using WebsiteHost.Api.Recording;
using WebsiteHost.Application;

namespace WebsiteHost;

public class BackEndForFrontEndModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof(RecordingApi).Assembly;

    public Assembly? DomainAssembly => null;

    public Dictionary<Type, string> AggregatePrefixes => new();

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get { return (_, services) => { services.RegisterUnshared<IRecordingApplication, RecordingApplication>(); }; }
    }

    public Action<WebApplication> ConfigureMiddleware
    {
        get { return app => app.RegisterRoutes(); }
    }
}