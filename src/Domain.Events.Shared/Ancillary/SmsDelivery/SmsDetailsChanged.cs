using Domain.Common;
using Domain.Common.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Ancillary.SmsDelivery;

public sealed class SmsDetailsChanged : DomainEvent
{
    public SmsDetailsChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public SmsDetailsChanged()
    {
    }

    public required string Body { get; set; }

    public required List<string> Tags { get; set; }

    public required string ToPhoneNumber { get; set; }
}