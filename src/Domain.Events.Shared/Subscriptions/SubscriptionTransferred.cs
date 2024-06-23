using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Subscriptions;

public sealed class SubscriptionTransferred : DomainEvent
{
    public SubscriptionTransferred(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public SubscriptionTransferred()
    {
    }

    public required string BuyerReference { get; set; }

    public required string FromBuyerId { get; set; }

    public required string OwningEntityId { get; set; }

    public required string PlanId { get; set; }

    public required string ProviderName { get; set; }

    public required Dictionary<string, string> ProviderState { get; set; }

    public required string SubscriptionReference { get; set; }

    public required string ToBuyerId { get; set; }
}