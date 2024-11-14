using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Ancillary.SmsDelivery;

public sealed class SendingAttempted : DomainEvent
{
    public SendingAttempted(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public SendingAttempted()
    {
    }

    public required DateTime When { get; set; }
}