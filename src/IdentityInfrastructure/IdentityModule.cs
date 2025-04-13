using System.Reflection;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Interfaces;
using Domain.Services.Shared;
using IdentityApplication;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain;
using IdentityDomain.DomainServices;
using IdentityInfrastructure.Api.PersonCredentials;
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
    public Action<WebApplication, List<MiddlewareRegistration>> ConfigureMiddleware
    {
        get { return (app, _) => app.RegisterRoutes(); }
    }

    public Assembly DomainAssembly => typeof(PersonCredentialRoot).Assembly;

    public Dictionary<Type, string> EntityPrefixes => new()
    {
        { typeof(PersonCredentialRoot), "pwdcred" },
        { typeof(MfaAuthenticator), "mfaauth" },
        { typeof(AuthTokensRoot), "authtok" },
        { typeof(APIKeyRoot), "apikey" },
        { typeof(SSOUserRoot), "ssocred" }
    };

    public Assembly InfrastructureAssembly => typeof(CredentialsApi).Assembly;

    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                services.AddSingleton<ITokensService, TokensService>();
                services.AddPerHttpRequest<IEmailAddressService, EmailAddressService>();
                services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
                services.AddSingleton<IMfaService>(c =>
                    new MfaService(
                        c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                        c.GetRequiredService<ITokensService>()));
                services.AddSingleton<IAPIKeyHasherService, APIKeyHasherService>();
                services.AddSingleton<IJWTTokensService>(c =>
                    new JWTTokensService(c.GetRequiredServiceForPlatform<IConfigurationSettings>(),
                        c.GetRequiredService<ITokensService>()));
                services.AddPerHttpRequest<IAuthTokensService, AuthTokensService>();
                services.AddSingleton<IEncryptionService>(c =>
                    new AesEncryptionService(c.GetRequiredServiceForPlatform<IConfigurationSettings>()
                        .GetString("ApplicationServices:SSOProvidersService:SSOUserTokens:AesSecret")));

                services.AddPerHttpRequest<IIdentityApplication, IdentityApplication.IdentityApplication>();
                services.AddPerHttpRequest<IAPIKeysApplication, APIKeysApplication>();
                services.AddPerHttpRequest<IAuthTokensApplication, AuthTokensApplication>();
                services.AddPerHttpRequest<IPersonCredentialsApplication, PersonCredentialsApplication>();
                services.AddPerHttpRequest<IMachineCredentialsApplication, MachineCredentialsApplication>();
                services.AddPerHttpRequest<ISingleSignOnApplication, SingleSignOnApplication>();
                services.AddPerHttpRequest<IPersonCredentialRepository>(c =>
                    new PersonCredentialRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IEventSourcingDddCommandStore<PersonCredentialRoot>>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterEventing<PersonCredentialRoot, PersonCredentialProjection>(
                    c => new PersonCredentialProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.AddPerHttpRequest<IAuthTokensRepository>(c =>
                    new AuthTokensRepository(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<ISnapshottingDddCommandStore<AuthTokensRoot>>(),
                        c.GetRequiredServiceForPlatform<IDataStore>()));
                services.RegisterEventing<AuthTokensRoot>();
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
                services.AddPerHttpRequest<ISSOService, SSOInProcessServiceClient>();
                services.AddPerHttpRequest<ISSOProvidersService, SSOProvidersService>();
                services.AddPerHttpRequest<IPersonCredentialsService, PersonCredentialsService>();

#if TESTINGONLY
                // EXTEND: replace these registrations with your own OAuth2 implementations
                services.AddSingleton<ISSOAuthenticationProvider, FakeSSOAuthenticationProvider>();
#endif
            };
        }
    }
}