namespace Application.Services.Shared;

/// <summary>
///     Defines an Identity Server provider, used to authenticate users and manage tokens
/// </summary>
public interface IIdentityServerProvider
{
    /// <summary>
    ///     Returns the credentials service for the provider
    /// </summary>
    public IIdentityServerCredentialsService CredentialsService { get; }

    /// <summary>
    ///     Returns the name of the provider
    /// </summary>
    string ProviderName { get; }
}