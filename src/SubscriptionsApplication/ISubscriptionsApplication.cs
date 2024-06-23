using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace SubscriptionsApplication;

public partial interface ISubscriptionsApplication
{
    Task<Result<SubscriptionWithPlan, Error>> CancelSubscriptionAsync(ICallerContext caller, string owningEntityId,
        CancellationToken cancellationToken);

    Task<Result<SubscriptionWithPlan, Error>> ChangePlanAsync(ICallerContext caller, string owningEntityId,
        string planId,
        CancellationToken cancellationToken);

    Task<Result<SearchResults<SubscriptionToMigrate>, Error>> ExportSubscriptionsToMigrateAsync(ICallerContext caller,
        SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken);

    Task<Result<SubscriptionWithPlan, Error>> ForceCancelSubscriptionAsync(ICallerContext caller, string owningEntityId,
        CancellationToken cancellationToken);

    Task<Result<SubscriptionWithPlan, Error>> GetSubscriptionAsync(ICallerContext caller, string owningEntityId,
        CancellationToken cancellationToken);

    Task<Result<SubscriptionWithPlan, Error>> GetSubscriptionPrivateAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    Task<Result<PricingPlans, Error>> ListPricingPlansAsync(ICallerContext caller, CancellationToken cancellationToken);

    Task<Result<SubscriptionWithPlan, Error>> MigrateSubscriptionAsync(ICallerContext caller, string? owningEntityId,
        string providerName, Dictionary<string, string> providerState, CancellationToken cancellationToken);

    Task<Result<SearchResults<Invoice>, Error>> SearchSubscriptionHistoryAsync(ICallerContext caller,
        string owningEntityId, DateTime? fromUtc, DateTime? toUtc, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken);

    Task<Result<SubscriptionWithPlan, Error>> TransferSubscriptionAsync(ICallerContext caller, string owningEntityId,
        string billingAdminId, CancellationToken cancellationToken);
}