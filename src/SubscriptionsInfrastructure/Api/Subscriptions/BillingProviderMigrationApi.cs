using Application.Resources.Shared;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Subscriptions;
using SubscriptionsApplication;

namespace SubscriptionsInfrastructure.Api.Subscriptions;

public class BillingProviderMigrationApi : IWebApiService
{
    private readonly ICallerContextFactory _callerFactory;
    private readonly ISubscriptionsApplication _subscriptionsApplication;

    public BillingProviderMigrationApi(ICallerContextFactory callerFactory,
        ISubscriptionsApplication subscriptionsApplication)
    {
        _callerFactory = callerFactory;
        _subscriptionsApplication = subscriptionsApplication;
    }

    public async Task<ApiSearchResult<SubscriptionToMigrate, ExportSubscriptionsToMigrateResponse>>
        ExportSubscriptionsToMigrate(ExportSubscriptionsToMigrateRequest request, CancellationToken cancellationToken)
    {
        var subscriptions =
            await _subscriptionsApplication.ExportSubscriptionsToMigrateAsync(_callerFactory.Create(),
                request.ToSearchOptions(), request.ToGetOptions(), cancellationToken);

        return () => subscriptions.HandleApplicationResult(subs =>
            new ExportSubscriptionsToMigrateResponse { Subscriptions = subs.Results, Metadata = subs.Metadata });
    }

    public async Task<ApiPutPatchResult<SubscriptionWithPlan, MigrateSubscriptionResponse>> MigrateSubscription(
        MigrateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        var subscription =
            await _subscriptionsApplication.MigrateSubscriptionAsync(_callerFactory.Create(), request.Id,
                request.ProviderName!, request.ProviderState, cancellationToken);

        return () =>
            subscription.HandleApplicationResult<SubscriptionWithPlan, MigrateSubscriptionResponse>(sub =>
                new MigrateSubscriptionResponse { Subscription = sub });
    }
}