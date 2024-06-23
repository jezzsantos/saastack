using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class BillingSubscribed : DomainEvent
{
    public BillingSubscribed(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public BillingSubscribed()
    {
    }

    public required string SubscriberId { get; set; }

    public required string SubscriptionId { get; set; }
}