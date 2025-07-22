using Application.Services.Shared;

namespace Infrastructure.Shared.ApplicationServices;

/// <summary>
///     Provides a native identity server provider that manages credentials, APIKeys and refresh tokens
/// </summary>
public class NativeIdentityServerProvider : IIdentityServerProvider
{
    public NativeIdentityServerProvider()
    {
        CredentialsService = new NativeIdentityServerCredentialsService();
    }

    public IIdentityServerCredentialsService CredentialsService { get; }

    public string ProviderName => NativeIdentityServerCredentialsService.Constants.ProviderName;
}

/// <summary>
///     Provides a credentials service for the native identity server provider
/// </summary>
public class NativeIdentityServerCredentialsService : IIdentityServerCredentialsService
{
    public static class Constants
    {
        public const string ProviderName = "native_identity_server_provider";
    }
}