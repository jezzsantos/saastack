using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Domain.Common.ValueObjects;
using Domain.Services.Shared;
using Domain.Shared.Subscriptions;

namespace IntegrationTesting.WebApi.Common.Stubs;

/// <summary>
///     Provides a stub for testing <see cref="IBillingProvider" />
/// </summary>
public class StubBillingProvider : IBillingProvider
{
    public StubBillingProvider()
    {
        StateInterpreter = new StubBillingStateInterpreter();
        GatewayService = new StubBillingGatewayService();
    }

    public IBillingGatewayService GatewayService { get; }

    public string ProviderName => StateInterpreter.ProviderName;

    public IBillingStateInterpreter StateInterpreter { get; }
}

/// <summary>
///     Provides a stub for testing <see cref="IBillingGatewayService" />
/// </summary>
public class StubBillingGatewayService : IBillingGatewayService
{
    public Task<Result<SubscriptionMetadata, Error>> CancelSubscriptionAsync(ICallerContext caller,
        CancelSubscriptionOptions options, BillingProvider provider,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SubscriptionMetadata, Error>>(provider.State);
    }

    public Task<Result<SubscriptionMetadata, Error>> ChangeSubscriptionPlanAsync(ICallerContext caller,
        ChangePlanOptions options, BillingProvider provider,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SubscriptionMetadata, Error>>(provider.State);
    }

    public Task<Result<PricingPlans, Error>> ListAllPricingPlansAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<PricingPlans, Error>>(new PricingPlans());
    }

    public Task<Result<SubscriptionMetadata, Error>> RestoreBuyerAsync(ICallerContext caller, SubscriptionBuyer buyer,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
        {
            { "BuyerId", buyer.Subscriber.EntityId }
        });
    }

    public Task<Result<List<Invoice>, Error>> SearchAllInvoicesAsync(ICallerContext caller, BillingProvider provider,
        DateTime fromUtc, DateTime toUtc,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<List<Invoice>, Error>>(new List<Invoice>());
    }

    public Task<Result<SubscriptionMetadata, Error>> SubscribeAsync(ICallerContext caller,
        SubscriptionBuyer buyer,
        SubscribeOptions options, CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
        {
            { "BuyerId", buyer.Subscriber.EntityId },
            { "SubscriptionId", Guid.NewGuid().ToString("N") },
            { "PlanId", "aplanid" }
        });
    }

    public Task<Result<SubscriptionMetadata, Error>> TransferSubscriptionAsync(ICallerContext caller,
        TransferSubscriptionOptions options, BillingProvider provider,
        CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SubscriptionMetadata, Error>>(provider.State);
    }
}

/// <summary>
///     Provides a stub for testing <see cref="IBillingStateInterpreter" />
/// </summary>
public class StubBillingStateInterpreter : IBillingStateInterpreter
{
    public Result<string, Error> GetBuyerReference(BillingProvider current)
    {
        return current.State["BuyerId"];
    }

    public Result<ProviderSubscription, Error> GetSubscriptionDetails(BillingProvider current)
    {
        if (!current.State.TryGetValue("SubscriptionId", out var subscriptionId))
        {
            return ProviderSubscription.Empty;
        }

        if (!current.State.TryGetValue("PlanId", out var planId))
        {
            return ProviderSubscription.Create(subscriptionId.ToId(), ProviderStatus.Empty,
                ProviderPlan.Empty, ProviderPlanPeriod.Empty, ProviderInvoice.Default, ProviderPaymentMethod.Empty);
        }

        return ProviderSubscription.Create(subscriptionId.ToId(),
            ProviderStatus.Create(BillingSubscriptionStatus.Activated, Optional<DateTime>.None, true).Value,
            ProviderPlan.Create(planId, BillingSubscriptionTier.Standard).Value,
            ProviderPlanPeriod.Empty, ProviderInvoice.Default, ProviderPaymentMethod.Empty);
    }

    public Result<Optional<string>, Error> GetSubscriptionReference(BillingProvider current)
    {
        return current.State.TryGetValue("SubscriptionId", out var subscriptionId)
            ? subscriptionId.ToOptional()
            : Optional<string>.None;
    }

    public string ProviderName => "testingonly_billing_provider";

    public Result<BillingProvider, Error> SetInitialProviderState(BillingProvider provider)
    {
        return provider;
    }
}