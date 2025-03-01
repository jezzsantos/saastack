using System.Reflection;
using Application.Services.Shared;
using {{SubdomainName | string.pascalplural}}Application;
using {{SubdomainName | string.pascalplural}}Application.Persistence;
using {{SubdomainName | string.pascalplural}}Domain;
using {{SubdomainName | string.pascalplural}}Infrastructure.Api.{{SubdomainName | string.pascalplural}};
using {{SubdomainName | string.pascalplural}}Infrastructure.ApplicationServices;
using {{SubdomainName | string.pascalplural}}Infrastructure.Persistence;
using {{SubdomainName | string.pascalplural}}Infrastructure.Persistence.ReadModels;
using Common;
using Domain.Interfaces;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {{SubdomainName | string.pascalplural}}Infrastructure;

public class {{SubdomainName | string.pascalplural}}Module : ISubdomainModule
{
    public Action<WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware
    {
        get { return (app, _) => app.RegisterRoutes(); }
    }

    public Assembly DomainAssembly => typeof({{SubdomainName | string.pascalsingular}}Root).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new()
    {
        { typeof({{SubdomainName | string.pascalsingular}}Root), "{{SubdomainName | string.pascalsingular | string.downcase}}" },
    };

    public Assembly InfrastructureAssembly => typeof({{SubdomainName | string.pascalplural}}Api).Assembly;

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                services.AddPerHttpRequest<I{{SubdomainName | string.pascalplural}}Application, {{SubdomainName | string.pascalplural}}Application.{{SubdomainName | string.pascalplural}}Application>();
                services.AddPerHttpRequest<I{{SubdomainName | string.pascalsingular}}Repository, {{SubdomainName | string.pascalsingular}}Repository>();
                services.RegisterEventing<{{SubdomainName | string.pascalsingular}}Root, {{SubdomainName | string.pascalsingular}}Projection>(
                    c => new {{SubdomainName | string.pascalsingular}}Projection(c.GetRequiredService<IRecorder>(), c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IDataStore>())
                );

                services.AddPerHttpRequest<I{{SubdomainName | string.pascalplural}}Service, {{SubdomainName | string.pascalplural}}InProcessServiceClient>();
            };
        }
    }
}