using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Organizations;

public sealed class BillingSubscriberChanged : DomainEvent
{
    public BillingSubscriberChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public BillingSubscriberChanged()
    {
    }

    public required string FromSubscriberId { get; set; }

    public required string ToSubscriberId { get; set; }
}