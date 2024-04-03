using Application.Persistence.Common;
using Common;
using Domain.Shared.Cars;
using QueryAny;

namespace CarsApplication.Persistence.ReadModels;

[EntityName("Unavailability")]
public class Unavailability : ReadModelEntity
{
    public Optional<string> CarId { get; set; }

    public UnavailabilityCausedBy CausedBy { get; set; } = UnavailabilityCausedBy.Other;

    public Optional<string> CausedByReference { get; set; }

    public Optional<DateTime> From { get; set; }

    public Optional<string> OrganizationId { get; set; }

    public Optional<DateTime> To { get; set; }
}