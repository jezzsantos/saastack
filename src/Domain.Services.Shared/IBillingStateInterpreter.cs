using Common;
using Domain.Shared.Subscriptions;

namespace Domain.Services.Shared;

/// <summary>
///     Defines an interpreter for a provider, that interprets the cached state for the billing management service
/// </summary>
public interface IBillingStateInterpreter
{
    /// <summary>
    ///     Returns the name of this provider
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    ///     Returns the provider's reference for the buyer from the <see cref="current" /> state
    /// </summary>
    Result<string, Error> GetBuyerReference(BillingProvider current);

    /// <summary>
    ///     Returns the provider's subscription from the <see cref="current" /> state
    /// </summary>
    Result<ProviderSubscription, Error> GetSubscriptionDetails(BillingProvider current);

    /// <summary>
    ///     Returns the provider's reference for the subscription from the <see cref="current" /> state
    /// </summary>
    Result<Optional<string>, Error> GetSubscriptionReference(BillingProvider current);

    /// <summary>
    ///     Creates the initial state of the newly subscribed provider
    /// </summary>
    Result<BillingProvider, Error> SetInitialProviderState(BillingProvider provider);
}