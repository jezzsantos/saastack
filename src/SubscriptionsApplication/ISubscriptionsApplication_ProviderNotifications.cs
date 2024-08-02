using Application.Interfaces;
using Common;

namespace SubscriptionsApplication;

public partial interface ISubscriptionsApplication
{
    Task<Result<SubscriptionMetadata, Error>> GetProviderStateForBuyerAsync(ICallerContext caller,
        string buyerReference, CancellationToken cancellationToken);

    Task<Result<SubscriptionMetadata, Error>> GetProviderStateForSubscriptionAsync(ICallerContext caller,
        string subscriptionReference, CancellationToken cancellationToken);

    Task<Result<Error>> NotifyBuyerDeletedAsync(ICallerContext caller, string providerName, SubscriptionMetadata state,
        CancellationToken cancellationToken);

    Task<Result<Error>> NotifyBuyerPaymentMethodChangedAsync(ICallerContext caller, string providerName,
        SubscriptionMetadata state, CancellationToken cancellationToken);

    Task<Result<Error>> NotifySubscriptionCancelledAsync(ICallerContext caller, string providerName,
        SubscriptionMetadata state, CancellationToken cancellationToken);

    Task<Result<Error>> NotifySubscriptionDeletedAsync(ICallerContext caller, string providerName,
        SubscriptionMetadata state, CancellationToken cancellationToken);

    Task<Result<Error>> NotifySubscriptionPlanChangedAsync(ICallerContext caller, string providerName,
        SubscriptionMetadata state, CancellationToken cancellationToken);
}