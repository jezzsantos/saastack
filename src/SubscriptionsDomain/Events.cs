using Domain.Common.ValueObjects;
using Domain.Events.Shared.Subscriptions;
using Domain.Shared.Subscriptions;
using Created = Domain.Events.Shared.Subscriptions.Created;
using Deleted = Domain.Events.Shared.Subscriptions.Deleted;

namespace SubscriptionsDomain;

public static class Events
{
    public static Created Created(Identifier id, Identifier owningEntityId, Identifier buyerId, string providerName)
    {
        return new Created(id)
        {
            OwningEntityId = owningEntityId,
            BuyerId = buyerId,
            ProviderName = providerName
        };
    }

    public static Deleted Deleted(Identifier id, Identifier deletedById)
    {
        return new Deleted(id, deletedById);
    }

    public static PaymentMethodChanged PaymentMethodChanged(Identifier id, Identifier owningEntityId,
        BillingProvider provider)
    {
        return new PaymentMethodChanged(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State
        };
    }

    public static ProviderChanged ProviderChanged(Identifier id, Identifier owningEntityId, string? fromProviderName,
        BillingProvider provider, string buyerReference, string subscriptionReference)
    {
        return new ProviderChanged(id)
        {
            OwningEntityId = owningEntityId,
            FromProviderName = fromProviderName,
            ToProviderName = provider.Name,
            ProviderState = provider.State,
            BuyerReference = buyerReference,
            SubscriptionReference = subscriptionReference
        };
    }

    public static SubscriptionCanceled SubscriptionCanceled(Identifier id, Identifier owningEntityId,
        BillingProvider provider)
    {
        return new SubscriptionCanceled(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State
        };
    }

    public static SubscriptionPlanChanged SubscriptionPlanChanged(Identifier id, Identifier owningEntityId,
        string planId, BillingProvider provider, string buyerReference, string subscriptionReference)
    {
        return new SubscriptionPlanChanged(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            BuyerReference = buyerReference,
            SubscriptionReference = subscriptionReference,
            PlanId = planId
        };
    }

    public static SubscriptionTransferred SubscriptionTransferred(Identifier id, Identifier owningEntityId,
        Identifier transfererId, Identifier transfereeId, string planId, BillingProvider provider,
        string buyerReference, string subscriptionReference)
    {
        return new SubscriptionTransferred(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State,
            BuyerReference = buyerReference,
            SubscriptionReference = subscriptionReference,
            PlanId = planId,
            FromBuyerId = transfererId,
            ToBuyerId = transfereeId
        };
    }

    public static SubscriptionUnsubscribed SubscriptionUnsubscribed(Identifier id, Identifier owningEntityId,
        BillingProvider provider)
    {
        return new SubscriptionUnsubscribed(id)
        {
            OwningEntityId = owningEntityId,
            ProviderName = provider.Name,
            ProviderState = provider.State
        };
    }
}