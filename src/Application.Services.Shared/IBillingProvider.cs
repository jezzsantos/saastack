using Domain.Services.Shared;

namespace Application.Services.Shared;

/// <summary>
///     Defines a billing provider, used to manage a billing subscription for a buyer of a company
/// </summary>
public interface IBillingProvider
{
    /// <summary>
    ///     Returns the gateway service for the provider
    /// </summary>
    public IBillingGatewayService GatewayService { get; }

    /// <summary>
    ///     returns the name of the provider
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    ///     Returns the interpreter to manage state changes to the provider
    /// </summary>
    public IBillingStateInterpreter StateInterpreter { get; }
}