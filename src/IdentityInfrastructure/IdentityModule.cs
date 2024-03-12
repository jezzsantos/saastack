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

public class IdentityModule : ISubDomainModule
{
    public Assembly ApiAssembly => typeof(PasswordCredentialsApi).Assembly;

    public Assembly DomainAssembly => typeof(PasswordCredentialRoot).Assembly;

    public Dictionary<Type, string> AggregatePrefixes => new()
    {
        { typeof(PasswordCredentialRoot), "pwdcred" },
        { typeof(AuthTokensRoot), "authtok" },
        { typeof(APIKeyRoot), "apikey" }
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
                services.AddSingleton<IEmailAddressService, EmailAddressService>();
                services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
                services.AddSingleton<IAPIKeyHasherService, APIKeyHasherService>();
                services.AddSingleton<IJWTTokensService>(c =>
                    new JWTTokensService(c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                        c.GetRequiredService<ITokensService>()));
                services.AddSingleton<IAuthTokensService, AuthTokensService>();
                services.AddSingleton<IEncryptionService>(c => new AesEncryptionService(c
                    .GetRequiredServiceForPlatform<IConfigurationSettings>()
                    .GetString("ApplicationServices:SSOProvidersService:SSOUserTokens:AesSecret")));

                services.AddSingleton<IAPIKeysApplication, APIKeysApplication>();
                services.AddSingleton<IAuthTokensApplication, AuthTokensApplication>();
                services.AddSingleton<IPasswordCredentialsApplication>(c => new PasswordCredentialsApplication(
                    c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IIdentifierFactory>(),
                    c.GetRequiredService<IEndUsersService>(),
                    c.GetRequiredService<INotificationsService>(),
                    c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                    c.GetRequiredService<IEmailAddressService>(),
                    c.GetRequiredService<ITokensService>(),
                    c.GetRequiredService<IPasswordHasherService>(),
                    c.GetRequiredService<IAuthTokensService>(),
                    c.GetRequiredService<IWebsiteUiService>(),
                    c.GetRequiredService<IPasswordCredentialsRepository>()));
                services.AddSingleton<IMachineCredentialsApplication, MachineCredentialsApplication>();
                services.AddSingleton<ISingleSignOnApplication, SingleSignOnApplication>();
                services.AddSingleton<IPasswordCredentialsRepository>(c => new PasswordCredentialsRepository(
                    c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcingDddCommandStore<PasswordCredentialRoot>>(),
                    c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<PasswordCredentialRoot, PasswordCredentialProjection>(
                    c => new PasswordCredentialProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.AddSingleton<IAuthTokensRepository>(c => new AuthTokensRepository(
                    c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcingDddCommandStore<AuthTokensRoot>>(),
                    c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<AuthTokensRoot, AuthTokensProjection>(
                    c => new AuthTokensProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.AddSingleton<IAPIKeysRepository>(c => new APIKeysRepository(
                    c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcingDddCommandStore<APIKeyRoot>>(),
                    c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<APIKeyRoot, APIKeyProjection>(
                    c => new APIKeyProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.AddSingleton<ISSOUsersRepository>(c => new SSOUsersRepository(
                    c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcingDddCommandStore<SSOUserRoot>>(),
                    c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<SSOUserRoot, SSOUserProjection>(
                    c => new SSOUserProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));

                services.AddSingleton<IAPIKeysService, APIKeysService>();
                services.AddSingleton<IIdentityService, IdentityInProcessServiceClient>();
                services.AddSingleton<ISSOProvidersService, SSOProvidersService>();
#if TESTINGONLY
                // EXTEND: replace these registrations with your own OAuth2 implementations
                services.AddSingleton<ISSOAuthenticationProvider, FakeSSOAuthenticationProvider>();
#endif
            };
        }
    }
}