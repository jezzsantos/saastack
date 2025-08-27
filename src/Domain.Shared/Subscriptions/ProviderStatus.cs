using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class ProviderStatus : ValueObjectBase<ProviderStatus>
{
    public static readonly ProviderStatus Empty =
        Create(BillingSubscriptionStatus.Unsubscribed, Optional<DateTime>.None, false).Value;

    public static Result<ProviderStatus, Error> Create(BillingSubscriptionStatus status,
        Optional<DateTime> canceledDateUtc, bool canBeUnsubscribed)
    {
        return new ProviderStatus(status, canceledDateUtc, canBeUnsubscribed);
    }

    private ProviderStatus(BillingSubscriptionStatus status,
        Optional<DateTime> canceledDateUtc, bool canBeUnsubscribed)
    {
        Status = status;
        CanceledDateUtc = canceledDateUtc;
        CanBeUnsubscribed = canBeUnsubscribed;
    }

    /// <summary>
    ///     Whether the subscription is in a state where it can be canceled.
    ///     Generally, not possible if the subscription is already canceled,
    ///     or in the process of being canceled, or if it is already unsubscribed.
    ///     ONly when it is activated.
    /// </summary>
    public bool CanBeCanceled => Status == BillingSubscriptionStatus.Activated;

    /// <summary>
    ///     Whether the subscription is in a state where it can be unsubscribed.
    ///     Generally, not possible if the subscription is already unsubscribed,
    ///     or in the process of being canceled, or if it is activated.
    ///     Only when it is canceled.
    /// </summary>
    public bool CanBeUnsubscribed { get; }

    public Optional<DateTime> CanceledDateUtc { get; }

    public BillingSubscriptionStatus Status { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<ProviderStatus> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new ProviderStatus(
                parts[0].Value.ToEnumOrDefault(BillingSubscriptionStatus.Unsubscribed),
                parts[1].ToOptional(val => val.FromIso8601()),
                parts[2].Value.ToBool());
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [Status, CanceledDateUtc, CanBeUnsubscribed];
    }
}

public enum BillingSubscriptionStatus
{
    Unsubscribed = 0,
    Activated = 1,
    Canceled = 2,
    Canceling = 3
}