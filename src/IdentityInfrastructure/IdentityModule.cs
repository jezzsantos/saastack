using System.Reflection;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Interfaces;
using Domain.Services.Shared.DomainServices;
using IdentityApplication;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain;
using IdentityDomain.DomainServices;
using IdentityInfrastructure.Api.PasswordCredentials;
using IdentityInfrastructure.ApplicationServices;
using IdentityInfrastructure.DomainServices;
using IdentityInfrastructure.Persistence;
using IdentityInfrastructure.Persistence.ReadModels;
using Infrastructure.Common.DomainServices;
using Infrastructure.Hosting.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Shared.DomainServices;
using Infrastructure.Web.Hosting.Common;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdentityInfrastructure;

public class IdentityModule : ISubdomainModule
{
    public Assembly InfrastructureAssembly => typeof(PasswordCredentialsApi).Assembly;

    public Assembly DomainAssembly => typeof(PasswordCredentialRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new()
    {
        { typeof(PasswordCredentialRoot), "pwdcred" },
        { typeof(AuthTokensRoot), "authtok" },
        { typeof(APIKeyRoot), "apikey" },
        { typeof(SSOUserRoot), "ssocred_" }
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
                services.AddSingleton<ITokensService, TokensService>();
                services.AddPerHttpRequest<IEmailAddressService, EmailAddressService>();
                services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
                services.AddSingleton<IAPIKeyHasherService, APIKeyHasherService>();
                services.AddSingleton<IJWTTokensService>(c =>
                    new JWTTokensService(c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                        c.GetRequiredService<ITokensService>()));
                services.AddPerHttpRequest<IAuthTokensService, AuthTokensService>();
                services.AddSingleton<IEncryptionService>(c =>
                    new AesEncryptionService(c.GetRequiredServiceForPlatform<IConfigurationSettings>()
                        .GetString("ApplicationServices:SSOProvidersService:SSOUserTokens:AesSecret")));

                services.AddPerHttpRequest<IAPIKeysApplication, APIKeysApplication>();
                services.AddPerHttpRequest<IAuthTokensApplication, AuthTokensApplication>();
                services.AddPerHttpRequest<IPasswordCredentialsApplication>(c =>
                    new PasswordCredentialsApplication(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IIdentifierFactory>(),
                        c.GetRequiredService<IEndUsersService>(),
                        c.GetRequiredService<IUserNotificationsService>(),
                        c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                        c.GetRequiredService<IEmailAddressService>(),
                        c.GetRequiredService<ITokensService>(),
                        c.GetRequiredService<IPasswordHasherService>(),
                        c.GetRequiredService<IAuthTokensService>(),
                        c.GetRequiredService<IWebsiteUiService>(),
                        c.GetRequiredService<IPasswordCredentialsRepository>()));
                services.AddPerHttpRequest<IMachineCredentialsApplication, MachineCredentialsApplication>();
                services.AddPerHttpRequest<ISingleSignOnApplication, SingleSignOnApplication>();
                services.AddPerHttpRequest<IPasswordCredentialsRepository>(c =>
                    new PasswordCredentialsRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IEventSourcingDddCommandStore<PasswordCredentialRoot>>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterEventing<PasswordCredentialRoot, PasswordCredentialProjection>(
                    c => new PasswordCredentialProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.AddPerHttpRequest<IAuthTokensRepository>(c =>
                    new AuthTokensRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IEventSourcingDddCommandStore<AuthTokensRoot>>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterEventing<AuthTokensRoot, AuthTokensProjection>(
                    c => new AuthTokensProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.AddPerHttpRequest<IAPIKeysRepository>(c =>
                    new APIKeysRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IEventSourcingDddCommandStore<APIKeyRoot>>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterEventing<APIKeyRoot, APIKeyProjection>(
                    c => new APIKeyProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.AddPerHttpRequest<ISSOUsersRepository>(c =>
                    new SSOUsersRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IEventSourcingDddCommandStore<SSOUserRoot>>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterEventing<SSOUserRoot, SSOUserProjection>(
                    c => new SSOUserProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));

                services.AddPerHttpRequest<IAPIKeysService, APIKeysService>();
                services.AddPerHttpRequest<IIdentityService, IdentityInProcessServiceClient>();
                services.AddPerHttpRequest<ISSOProvidersService, SSOProvidersService>();
#if TESTINGONLY
                // EXTEND: replace these registrations with your own OAuth2 implementations
                services.AddSingleton<ISSOAuthenticationProvider, FakeSSOAuthenticationProvider>();
#endif
            };
        }
    }
}