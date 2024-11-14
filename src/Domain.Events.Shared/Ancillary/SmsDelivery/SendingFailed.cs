using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Ancillary.SmsDelivery;

public sealed class SendingFailed : DomainEvent
{
    public SendingFailed(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public SendingFailed()
    {
    }

    public required DateTime When { get; set; }
}