using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Subscriptions;

public sealed class ProviderChanged : DomainEvent
{
    public ProviderChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public ProviderChanged()
    {
    }

    public required string BuyerReference { get; set; }

    public string? FromProviderName { get; set; }

    public required string OwningEntityId { get; set; }

    public required Dictionary<string, string> ProviderState { get; set; }

    public string? SubscriptionReference { get; set; }

    public required string ToProviderName { get; set; }
}