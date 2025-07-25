namespace Application.Services.Shared;

/// <summary>
///     Defines an Identity Server provider, used to authenticate and authorize users
/// </summary>
public interface IIdentityServerProvider
{
    /// <summary>
    ///     Returns the API Keys service for the provider
    /// </summary>
    public IIdentityServerApiKeyService ApiKeyService { get; }

    /// <summary>
    ///     Returns the credentials service for the provider
    /// </summary>
    public IIdentityServerCredentialsService CredentialsService { get; }

    /// <summary>
    ///     Returns the OIDC service for the provider
    /// </summary>
    public IIdentityServerOpenIdConnectService OpenIdConnectService { get; }

    /// <summary>
    ///     Returns the name of the provider
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    ///     Returns the SSO service for the provider
    /// </summary>
    public IIdentityServerSingleSignOnService SingleSignOnService { get; }
}