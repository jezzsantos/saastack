using System.Reflection;
using Common;
using Domain.Interfaces;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SigningsApplication;
using SigningsApplication.Persistence;
using SigningsDomain;
using SigningsInfrastructure.Api.Signings;
using SigningsInfrastructure.Notifications;
using SigningsInfrastructure.Persistence;
using SigningsInfrastructure.Persistence.ReadModels;

namespace SigningsInfrastructure;

public class SigningModule : ISubdomainModule
{
    public Assembly InfrastructureAssembly => typeof(SigningApi).Assembly;

    public Assembly DomainAssembly => typeof(SigningRequestRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new()
    {
        { typeof(SigningRequestRoot), "signreq" }
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
                services.AddPerHttpRequest<ISigningsApplication, SigningsApplication.SigningsApplication>();
                services.AddPerHttpRequest<ISigningRepository, SigningRepository>();
                services.RegisterEventing<SigningRequestRoot, SigningProjection, SigningNotifier>(
                    c => new SigningProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IDataStore>()),
                    _ => new SigningNotifier()
                );
            };
        }
    }
}