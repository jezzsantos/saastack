using System.Reflection;
using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Configuration;
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
                services.RegisterUnshared<IEmailAddressService, EmailAddressService>();
                services.RegisterUnshared<IPasswordHasherService, PasswordHasherService>();
                services.RegisterUnshared<IAPIKeyHasherService, APIKeyHasherService>();
                services.RegisterUnshared<IJWTTokensService, JWTTokensService>();
                services.RegisterUnshared<IAuthTokensService, AuthTokensService>();
                services.RegisterUnshared<IEncryptionService>(c => new AesEncryptionService(c
                    .ResolveForUnshared<IConfigurationSettings>().Platform
                    .GetString("ApplicationServices:SSOProvidersService:SSOUserTokens:AesSecret")));

                services.RegisterUnshared<IAPIKeysApplication, APIKeysApplication>();
                services.RegisterUnshared<IAuthTokensApplication, AuthTokensApplication>();
                services.RegisterUnshared<IPasswordCredentialsApplication, PasswordCredentialsApplication>();
                services.RegisterUnshared<IMachineCredentialsApplication, MachineCredentialsApplication>();
                services.RegisterUnshared<ISingleSignOnApplication, SingleSignOnApplication>();
                services.RegisterUnshared<IPasswordCredentialsRepository>(c => new PasswordCredentialsRepository(
                    c.ResolveForUnshared<IRecorder>(),
                    c.ResolveForUnshared<IDomainFactory>(),
                    c.ResolveForUnshared<IEventSourcingDddCommandStore<PasswordCredentialRoot>>(),
                    c.ResolveForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<PasswordCredentialRoot, PasswordCredentialProjection>(
                    c => new PasswordCredentialProjection(c.ResolveForUnshared<IRecorder>(),
                        c.ResolveForUnshared<IDomainFactory>(),
                        c.ResolveForPlatform<IDataStore>()));
                services.RegisterUnshared<IAuthTokensRepository>(c => new AuthTokensRepository(
                    c.ResolveForUnshared<IRecorder>(),
                    c.ResolveForUnshared<IDomainFactory>(),
                    c.ResolveForUnshared<IEventSourcingDddCommandStore<AuthTokensRoot>>(),
                    c.ResolveForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<AuthTokensRoot, AuthTokensProjection>(
                    c => new AuthTokensProjection(c.ResolveForUnshared<IRecorder>(),
                        c.ResolveForUnshared<IDomainFactory>(),
                        c.ResolveForPlatform<IDataStore>()));
                services.RegisterUnshared<IAPIKeysRepository>(c => new APIKeysRepository(
                    c.ResolveForUnshared<IRecorder>(),
                    c.ResolveForUnshared<IDomainFactory>(),
                    c.ResolveForUnshared<IEventSourcingDddCommandStore<APIKeyRoot>>(),
                    c.ResolveForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<APIKeyRoot, APIKeyProjection>(
                    c => new APIKeyProjection(c.ResolveForUnshared<IRecorder>(),
                        c.ResolveForUnshared<IDomainFactory>(),
                        c.ResolveForPlatform<IDataStore>()));
                services.RegisterUnshared<ISSOUsersRepository>(c => new SSOUsersRepository(
                    c.ResolveForUnshared<IRecorder>(),
                    c.ResolveForUnshared<IDomainFactory>(),
                    c.ResolveForUnshared<IEventSourcingDddCommandStore<SSOUserRoot>>(),
                    c.ResolveForPlatform<IDataStore>()));
                services.RegisterUnTenantedEventing<SSOUserRoot, SSOUserProjection>(
                    c => new SSOUserProjection(c.ResolveForUnshared<IRecorder>(),
                        c.ResolveForUnshared<IDomainFactory>(),
                        c.ResolveForPlatform<IDataStore>()));

                services.RegisterUnshared<IAPIKeysService, APIKeysService>();
                services.RegisterUnshared<IIdentityService, IdentityInProcessServiceClient>();
                services.RegisterUnshared<ISSOProvidersService, SSOProvidersService>();
#if TESTINGONLY
                // EXTEND: replace these registrations with your own OAuth2 implementations
                services.RegisterUnshared<ISSOAuthenticationProvider, FakeSSOAuthenticationProvider>();
#endif
            };
        }
    }
}