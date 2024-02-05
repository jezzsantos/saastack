using System.Reflection;
using Application.Interfaces.Services;
using Infrastructure.Web.Hosting.Common;
using Common;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Hosting.Common;
using Infrastructure.Web.Hosting.Common.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ProjectName;

public class {SubDomainName}sModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof({SubDomainName}sApi).Assembly;

    public Assembly DomainAssembly => typeof({SubDomainName}Root).Assembly;

    public Dictionary<Type, string> AggregatePrefixes => new()
    {
        { typeof({SubDomainName}Root), "{SubDomainNameLower}" }
    };

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
                services.RegisterTenanted<I{SubDomainName}sApplication, {SubDomainName}sApplication.{SubDomainName}sApplication>();
                services.RegisterTenanted<I{SubDomainName}Repository, {SubDomainName}Repository>();
                services.RegisterTenantedEventing<{SubDomainName}Root, {SubDomainName}Projection>(
                    c => new {SubDomainName}Projection(c.ResolveForUnshared<IRecorder>(), c.ResolveForUnshared<IDomainFactory>(),
                        c.ResolveForTenant<IDataStore>())
                );
            };
        }
    }
}