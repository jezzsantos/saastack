using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Subscriptions;

public sealed class SubscriptionUnsubscribed : DomainEvent
{
    public SubscriptionUnsubscribed(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public SubscriptionUnsubscribed()
    {
    }

    public required string OwningEntityId { get; set; }

    public required string ProviderName { get; set; }

    public required Dictionary<string, string> ProviderState { get; set; }
}