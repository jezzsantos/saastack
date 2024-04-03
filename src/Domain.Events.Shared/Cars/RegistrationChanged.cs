using Domain.Common;
using Domain.Common.ValueObjects;
using Domain.Shared.Cars;
using JetBrains.Annotations;

namespace Domain.Events.Shared.Cars;

public sealed class RegistrationChanged : DomainEvent
{
    public RegistrationChanged(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public RegistrationChanged()
    {
    }

    public required string Jurisdiction { get; set; }

    public required string Number { get; set; }

    public required string OrganizationId { get; set; }

    public required CarStatus Status { get; set; }
}