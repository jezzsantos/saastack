using System.Reflection;
using {SubDomainName}sApplication;
using {SubDomainName}sApplication.Persistence;
using {SubDomainName}sDomain;
using {SubDomainName}sInfrastructure.Persistence;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectName;

public class ProjectNameModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof(Apis.{SubDomainName}s.ProjectName).Assembly;

    public Dictionary<Type, string> AggregatePrefixes => new()
    {
        { typeof({SubDomainName}Root), "car" },
        { typeof(UnavailabilityEntity), "unavail" }
    };

    public Action<WebApplication> MinimalApiRegistrationFunction
    {
        get { return app => app.RegisterRoutes(); }
    }

    public Action<ConfigurationManager, IServiceCollection> RegisterServicesFunction
    {
        get
        {
            return (_, services) =>
            {
                services.AddScoped<I{SubDomainName}sApplication, {SubDomainName}sApplication.{SubDomainName}sApplication>();
                services.AddScoped<I{SubDomainName}Repository, {SubDomainName}Repository>();
            };
        }
    }
}