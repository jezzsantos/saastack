using Application.Services.Shared;
using Common;
using Domain.Common.Identity;
using Domain.Services.Shared;
using IdentityApplication.ApplicationServices;
using IdentityApplication.Persistence;
using IdentityDomain.DomainServices;

namespace IdentityInfrastructure.ApplicationServices;

/// <summary>
///     Provides a native identity server provider that manages OIDC, Credentials, and APIKeys
/// </summary>
public class NativeIdentityServerProvider : IIdentityServerProvider
{
    public NativeIdentityServerProvider(IRecorder recorder, IIdentifierFactory identifierFactory,
        ITokensService tokensService, IAPIKeyHasherService apiKeyHasherService, IEndUsersService endUsersService,
        IUserProfilesService userProfilesService, IAPIKeysRepository repository)
    {
        CredentialsService = new NativeIdentityServerCredentialsService();
        OidcService = new NativeIdentityServerOpenIdConnectService();
        ApiKeyService = new NativeIdentityServerApiKeyService(recorder, identifierFactory, tokensService,
            apiKeyHasherService, endUsersService, userProfilesService, repository);
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