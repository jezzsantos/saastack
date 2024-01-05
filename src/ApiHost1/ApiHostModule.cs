using System.Reflection;
using AncillaryInfrastructure.Api.Usages;
using Application.Persistence.Shared;
using Application.Services.Shared;
using Common;
using Domain.Services.Shared.DomainServices;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.Shared.ApplicationServices;
using Infrastructure.Shared.ApplicationServices;
using Infrastructure.Web.Hosting.Common;

namespace ApiHost1;

/// <summary>
///     Provides a module for common services of a API host
/// </summary>
public class ApiHostModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof(UsagesApi).Assembly;

    public Assembly DomainAssembly => null!;

    public Dictionary<Type, string> AggregatePrefixes => new();

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                services.RegisterUnshared<IEmailMessageQueueRepository>(c =>
                    new EmailMessageQueueRepository(c.Resolve<IRecorder>(), c.ResolveForPlatform<IQueueStore>()));

                services.RegisterUnshared<ITokensService, TokensService>();
                services.RegisterUnshared<INotificationsService, EmailNotificationsService>();
                services.RegisterUnshared<IWebsiteUiService, WebsiteUiService>();
                services.RegisterUnshared<IEmailSendingService, EmailSendingService>();
            };
        }
    }

    public Action<WebApplication> ConfigureMiddleware
    {
        get { return _ => { }; }
    }
}