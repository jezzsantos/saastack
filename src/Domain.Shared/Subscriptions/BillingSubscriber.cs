using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Shared.Subscriptions;

public sealed class BillingSubscriber : ValueObjectBase<BillingSubscriber>
{
    public static Result<BillingSubscriber, Error> Create(string subscriptionId, string subscriberId)
    {
        if (subscriptionId.IsNotValuedParameter(nameof(subscriptionId), out var error1))
        {
            return error1;
        }

        if (subscriberId.IsNotValuedParameter(nameof(subscriberId), out var error2))
        {
            return error2;
        }

        return new BillingSubscriber(subscriptionId, subscriberId);
    }

    private BillingSubscriber(string subscriptionId, string subscriberId)
    {
        SubscriptionId = subscriptionId;
        SubscriberId = subscriberId;
    }

    public string SubscriberId { get; }

    public string SubscriptionId { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<BillingSubscriber> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, false);
            return new BillingSubscriber(parts[0]!, parts[1]!);
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return [SubscriptionId, SubscriberId];
    }

    public BillingSubscriber ChangeSubscriber(Identifier subscriberId)
    {
        return new BillingSubscriber(SubscriptionId, subscriberId);
    }
}