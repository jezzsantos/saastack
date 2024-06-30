using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Subscriptions;

public sealed class SubscriptionPlanChanged : DomainEvent
{
    public SubscriptionPlanChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public SubscriptionPlanChanged()
    {
    }

    public required string BuyerReference { get; set; }

    public required string OwningEntityId { get; set; }

    public required string PlanId { get; set; }

    public required string ProviderName { get; set; }

    public required Dictionary<string, string> ProviderState { get; set; }

    public required string SubscriptionReference { get; set; }
}