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

    public Action<WebApplication> MinimalApiRegistrationFunction
    {
        get { return app => app.RegisterRoutes(); }
    }

    public Action<ConfigurationManager, IServiceCollection> RegisterServicesFunction
    {
        get { return (_, services) => { services.RegisterUnshared<IRecordingApplication, RecordingApplication>(); }; }
    }
}