using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;
using Domain.Services.Shared;
using Domain.Shared.Subscriptions;
using Infrastructure.Shared.ApplicationServices.External;
using Subscription = ChargeBee.Models.Subscription;

namespace SubscriptionsInfrastructure.IntegrationTests.Stubs;

/// <summary>
///     In this stub we can use the real <see cref="ChargebeeStateInterpreter" />,
///     but we have to stub out the behaviour of the <see cref="IBillingGatewayService" /> for Chargebee
/// </summary>
public class StubChargebeeBillingProvider : IBillingProvider
{
    public StubChargebeeBillingProvider(IConfigurationSettings settings)
    {
        StateInterpreter = new ChargebeeStateInterpreter(settings);
        GatewayService = new StubBillingGatewayService();
    }

    public IBillingGatewayService GatewayService { get; }

    public string ProviderName => StateInterpreter.ProviderName;

    public IBillingStateInterpreter StateInterpreter { get; }
}

public class StubBillingGatewayService : IBillingGatewayService
{
    private const string InitialPlanId = "apaidtrial"; //see appsettings.Testing.json

    public Task<Result<SubscriptionMetadata, Error>> CancelSubscriptionAsync(ICallerContext caller,
        CancelSubscriptionOptions options, BillingProvider provider,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow.ToNearestSecond();
        var cancellationDate = options.CancelWhen == CancelSubscriptionSchedule.Immediately
            ? now
            : now.AddMonths(1);
        var status = options.CancelWhen == CancelSubscriptionSchedule.Immediately
            ? Subscription.StatusEnum.Cancelled
            : Subscription.StatusEnum.NonRenewing;
        var metadata = new SubscriptionMetadata(provider.State)
        {
            [ChargebeeConstants.MetadataProperties.SubscriptionStatus] = status.ToString().ToCamelCase(),
            [ChargebeeConstants.MetadataProperties.CanceledAt] = cancellationDate.ToIso8601()
        };

        return Task.FromResult<Result<SubscriptionMetadata, Error>>(metadata);
    }

    public Task<Result<SubscriptionMetadata, Error>> ChangeSubscriptionPlanAsync(ICallerContext caller,
        ChangePlanOptions options, BillingProvider provider, CancellationToken cancellationToken)
    {
        var metadata = new SubscriptionMetadata(provider.State)
        {
            [ChargebeeConstants.MetadataProperties.PlanId] = options.PlanId,
            [ChargebeeConstants.MetadataProperties.SubscriptionStatus] =
                Subscription.StatusEnum.Active.ToString().ToCamelCase()
        };
        metadata.Remove(ChargebeeConstants.MetadataProperties.CanceledAt);
        metadata.Remove(ChargebeeConstants.MetadataProperties.TrialEnd);
        metadata.Remove(ChargebeeConstants.MetadataProperties.SubscriptionDeleted);

        return Task.FromResult<Result<SubscriptionMetadata, Error>>(metadata);
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
            { ChargebeeConstants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId }
        });
    }

    public Task<Result<List<Invoice>, Error>> SearchAllInvoicesAsync(ICallerContext caller, BillingProvider provider,
        DateTime fromUtc, DateTime toUtc, SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<List<Invoice>, Error>>(new List<Invoice>());
    }

    public Task<Result<SubscriptionMetadata, Error>> SubscribeAsync(ICallerContext caller,
        SubscriptionBuyer buyer, SubscribeOptions options, CancellationToken cancellationToken)
    {
        return Task.FromResult<Result<SubscriptionMetadata, Error>>(new SubscriptionMetadata
        {
            { ChargebeeConstants.MetadataProperties.CustomerId, buyer.Subscriber.EntityId },
            { ChargebeeConstants.MetadataProperties.SubscriptionId, CreateSubscriptionId() },
#if TESTINGONLY
            { ChargebeeConstants.MetadataProperties.PlanId, options.PlanId ?? InitialPlanId },
#endif
            {
                ChargebeeConstants.MetadataProperties.SubscriptionStatus,
                Subscription.StatusEnum.Active.ToString().ToCamelCase()
            },
            {
                ChargebeeConstants.MetadataProperties.BillingPeriodUnit,
                Subscription.BillingPeriodUnitEnum.Month.ToString().ToCamelCase()
            },
            { ChargebeeConstants.MetadataProperties.BillingPeriodValue, "1" }
        });
    }

    public Task<Result<SubscriptionMetadata, Error>> TransferSubscriptionAsync(ICallerContext caller,
        TransferSubscriptionOptions options, BillingProvider provider, CancellationToken cancellationToken)
    {
        var planId = options.PlanId ?? provider.State[ChargebeeConstants.MetadataProperties.PlanId];

        var metadata = new SubscriptionMetadata(provider.State)
        {
            [ChargebeeConstants.MetadataProperties.PlanId] = planId,
            [ChargebeeConstants.MetadataProperties.SubscriptionStatus] =
                Subscription.StatusEnum.Active.ToString().ToCamelCase()
        };
        metadata.Remove(ChargebeeConstants.MetadataProperties.CanceledAt);
        metadata.Remove(ChargebeeConstants.MetadataProperties.TrialEnd);
        metadata.Remove(ChargebeeConstants.MetadataProperties.SubscriptionDeleted);

        return Task.FromResult<Result<SubscriptionMetadata, Error>>(metadata);
    }

    private static string CreateSubscriptionId()
    {
        return Guid.NewGuid().ToString("N");
    }
}