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
///     Provides a native identity server provider that manages OIDC, Person Credentials, and APIKeys
/// </summary>
public class NativeIdentityServerProvider : IIdentityServerProvider
{
    public NativeIdentityServerProvider(IRecorder recorder, IIdentifierFactory identifierFactory,
        IConfigurationSettings settings, ITokensService tokensService, IAPIKeyHasherService apiKeyHasherService,
        IEndUsersService endUsersService, IUserProfilesService userProfilesService,
        IUserNotificationsService userNotificationsService, IEmailAddressService emailAddressService,
        IEncryptionService encryptionService, IPasswordHasherService passwordHasherService, IMfaService mfaService,
        IAuthTokensService authTokensService, IWebsiteUiService websiteUiService, IAPIKeysRepository apiKeysRepository,
        IPersonCredentialRepository credentialsRepository)
    {
        CredentialsService = new NativeIdentityServerCredentialsService(recorder, identifierFactory, endUsersService,
            userProfilesService, userNotificationsService, settings, emailAddressService, tokensService,
            encryptionService, passwordHasherService, mfaService, authTokensService, websiteUiService,
            credentialsRepository);
        OidcService = new NativeIdentityServerOpenIdConnectService();
        ApiKeyService = new NativeIdentityServerApiKeyService(recorder, identifierFactory, tokensService,
            apiKeyHasherService, endUsersService, userProfilesService, apiKeysRepository);
    }

    public IIdentityServerApiKeyService ApiKeyService { get; }

    public IIdentityServerCredentialsService CredentialsService { get; }

    public IIdentityServerOpenIdConnectService OidcService { get; }

    public string ProviderName => Constants.ProviderName;

    public static class Constants
    {
        public const string ProviderName = "native_identity_provider";
    }
}