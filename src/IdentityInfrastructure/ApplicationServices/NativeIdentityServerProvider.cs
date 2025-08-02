using Application.Services.Shared;
using Common;
using Common.Configuration;
using Domain.Common.Identity;
using Domain.Services.Shared;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain.DomainServices;

namespace IdentityInfrastructure.ApplicationServices;

/// <summary>
///     Provides a native identity server provider that manages OIDC, Person Credentials, API Keys and Single Sign On
///     by self-persisting identity state
/// </summary>
public class NativeIdentityServerProvider : IIdentityServerProvider
{
    public NativeIdentityServerProvider(IRecorder recorder, IIdentifierFactory identifierFactory,
        IConfigurationSettings settings, ITokensService tokensService, IAPIKeyHasherService apiKeyHasherService,
        IEndUsersService endUsersService, IUserProfilesService userProfilesService,
        IUserNotificationsService userNotificationsService, IEmailAddressService emailAddressService,
        IEncryptionService encryptionService, IPasswordHasherService passwordHasherService, IMfaService mfaService,
        IAuthTokensService authTokensService, ISSOProvidersService ssoProvidersService,
        IWebsiteUiService websiteUiService, IOAuth2ClientService oauth2ClientService,
        IAPIKeysRepository apiKeysRepository, IPersonCredentialRepository credentialsRepository,
        IOAuth2ClientRepository oAuthClientRepository, IOAuth2ClientConsentRepository oAuthClientConsentRepository,
        IOpenIdConnectAuthorizationRepository oidcAuthorizationRepository)
    {
        CredentialsService = new NativeIdentityServerCredentialsService(recorder, identifierFactory, endUsersService,
            userProfilesService, userNotificationsService, settings, emailAddressService, tokensService,
            encryptionService, passwordHasherService, mfaService, authTokensService, websiteUiService,
            credentialsRepository);
        OpenIdConnectService =
            new NativeIdentityServerOpenIdConnectService(recorder, identifierFactory, settings, encryptionService,
                tokensService, websiteUiService, oauth2ClientService, authTokensService, endUsersService,
                userProfilesService, oidcAuthorizationRepository);
        ApiKeyService = new NativeIdentityServerApiKeyService(recorder, identifierFactory, tokensService,
            apiKeyHasherService, endUsersService, userProfilesService, apiKeysRepository);
        SingleSignOnService = new NativeIdentityServerSingleSignOnService(recorder, endUsersService,
            ssoProvidersService, authTokensService);
        OAuth2ClientService =
            new NativeIdentityServerOAuth2ClientService(recorder, identifierFactory, tokensService,
                passwordHasherService, oAuthClientRepository, oAuthClientConsentRepository);
    }

    public IIdentityServerApiKeyService ApiKeyService { get; }

    public IIdentityServerCredentialsService CredentialsService { get; }

    public IIdentityServerOAuth2ClientService OAuth2ClientService { get; }

    public IIdentityServerOpenIdConnectService OpenIdConnectService { get; }

    public string ProviderName => Constants.ProviderName;

    public IIdentityServerSingleSignOnService SingleSignOnService { get; }

    public static class Constants
    {
        public const string ProviderName = "native_identity_provider";
    }
}