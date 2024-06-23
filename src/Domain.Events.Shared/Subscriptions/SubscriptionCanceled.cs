#region

using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

#endregion

namespace Domain.Events.Shared.Subscriptions;

public sealed class SubscriptionCanceled : DomainEvent
{
    public SubscriptionCanceled(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public SubscriptionCanceled()
    {
    }

    public required string OwningEntityId { get; set; }

    public required string ProviderName { get; set; }

    public required Dictionary<string, string> ProviderState { get; set; }
}