using System.Reflection;
using Application.Interfaces.Services;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Interfaces;
using ImagesApplication;
using ImagesApplication.Persistence;
using ImagesDomain;
using ImagesInfrastructure.Api.Images;
using ImagesInfrastructure.ApplicationServices;
using ImagesInfrastructure.Notifications;
using ImagesInfrastructure.Persistence;
using ImagesInfrastructure.Persistence.ReadModels;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Api.Common;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ImagesInfrastructure;

public class ImagesModule : ISubdomainModule
{
    public Action<WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware
    {
        get { return (app, _) => app.RegisterRoutes(); }
    }

    public Assembly DomainAssembly => typeof(ImageRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new()
    {
        { typeof(ImageRoot), "image" }
    };

    public Assembly InfrastructureAssembly => typeof(ImagesApi).Assembly;

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                services.AddSingleton<IFileUploadService, FileUploadService>();
                services.AddPerHttpRequest<IImagesApplication>(c =>
                    new ImagesApplication.ImagesApplication(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IIdentifierFactory>(),
                        c.GetRequiredService<IHostSettings>(),
                        c.GetRequiredService<IImagesRepository>()));
                services.AddPerHttpRequest<IImagesRepository>(c =>
                    new ImagesRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IEventSourcingDddCommandStore<ImageRoot>>(),
                        c.GetRequiredServiceForPlatform<IBlobStore>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterEventing<ImageRoot, ImageProjection, ImageNotifier>(
                    c => new ImageProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()),
                    _ => new ImageNotifier());

                services.AddPerHttpRequest<IImagesService, ImagesInProcessServiceClient>();
            };
        }
    }
}