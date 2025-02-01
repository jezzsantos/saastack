using System.Reflection;
using Domain.Interfaces;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ProjectName.Persistence;
using ProjectName.Persistence.Notifications;
using ProjectName.Persistence.ReadModels;

namespace ProjectName;

public class {SubDomainName}sModule : ISubdomainModule
{
    public Assembly InfrastructureAssembly => typeof({SubDomainName}sApi).Assembly;

    public Assembly? DomainAssembly => typeof({SubDomainName}Root).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new()
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
                services.AddPerHttpRequest<I{SubDomainName}sApplication, {SubDomainName}sApplication.{SubDomainName}sApplication>();
                services.AddPerHttpRequest<I{SubDomainName}Repository, {SubDomainName}Repository>();
                services.RegisterEventing<{SubDomainName}Root, {SubDomainName}Projection>(
                    c => new {SubDomainName}Projection(c.GetRequiredService<IRecorder>(), c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IDataStore>()),
                    _ => new {SubDomainName}Notifier()
                );
            };
        }
    }
}