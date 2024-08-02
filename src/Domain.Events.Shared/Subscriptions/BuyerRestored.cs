using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Subscriptions;

public sealed class BuyerRestored : DomainEvent
{
    public BuyerRestored(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public BuyerRestored()
    {
    }

    public required string OwningEntityId { get; set; }

    public required string ProviderName { get; set; }

    public required Dictionary<string, string> ProviderState { get; set; }

    public required string BuyerReference { get; set; }

    public string? SubscriptionReference { get; set; }
}