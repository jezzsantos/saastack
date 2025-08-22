using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class ProviderPlan : ValueObjectBase<ProviderPlan>
{
    public static readonly ProviderPlan Empty =
        new(Optional<string>.None, false, Optional<DateTime>.None, BillingSubscriptionTier.Unsubscribed);

    public static Result<ProviderPlan, Error> Create(string planId, BillingSubscriptionTier tier)
    {
        if (planId.IsInvalidParameter(id => id.HasValue(), nameof(planId), Resources.ProviderPlan_InvalidPlanId,
                out var error))
        {
            return error;
        }

        return Create(planId, false, Optional<DateTime>.None, tier);
    }

    public static Result<ProviderPlan, Error> Create(string planId, bool isTrial, Optional<DateTime> trialEndDateUtc,
        BillingSubscriptionTier tier)
    {
        if (planId.IsInvalidParameter(id => id.HasValue(), nameof(planId), Resources.ProviderPlan_InvalidPlanId,
                out var error))
        {
            return error;
        }

        return new ProviderPlan(planId, isTrial, trialEndDateUtc, tier);
    }

    private ProviderPlan(Optional<string> planId, bool isTrial, Optional<DateTime> trialEndDateUtc,
        BillingSubscriptionTier tier)
    {
        PlanId = planId;
        IsTrial = isTrial;
        TrialEndDateUtc = trialEndDateUtc;
        Tier = tier;
    }

    public bool IsTrial { get; }

    public Optional<string> PlanId { get; }

    public BillingSubscriptionTier Tier { get; }

    public Optional<DateTime> TrialEndDateUtc { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderPlan> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new ProviderPlan(
                parts[0],
                parts[1].Value.ToBool(),
                parts[2].ToOptional(val => val.FromIso8601()),
                parts[3].Value.ToEnumOrDefault(BillingSubscriptionTier.Unsubscribed));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [PlanId, IsTrial, TrialEndDateUtc, Tier];
    }
}

public enum BillingSubscriptionTier
{
    // EXTEND: define other subscription tiers related to subscription plans
    Unsubscribed = 0,
    Standard = 1,
    Professional = 2,
    Enterprise = 3
}