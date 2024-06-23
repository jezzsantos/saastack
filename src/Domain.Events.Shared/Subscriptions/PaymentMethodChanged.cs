using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Subscriptions;

public sealed class PaymentMethodChanged : DomainEvent
{
    public PaymentMethodChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public PaymentMethodChanged()
    {
    }

    public required string OwningEntityId { get; set; }

    public required string ProviderName { get; set; }

    public required Dictionary<string, string> ProviderState { get; set; }
}