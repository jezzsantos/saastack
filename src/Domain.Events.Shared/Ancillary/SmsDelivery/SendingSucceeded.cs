using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Ancillary.SmsDelivery;

public sealed class SendingSucceeded : DomainEvent
{
    public SendingSucceeded(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public SendingSucceeded()
    {
    }

    public required string? ReceiptId { get; set; }

    public required DateTime When { get; set; }
}