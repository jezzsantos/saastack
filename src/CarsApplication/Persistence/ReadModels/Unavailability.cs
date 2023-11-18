using Application.Persistence.Common;
using CarsDomain;
using Common;
using QueryAny;

namespace CarsApplication.Persistence.ReadModels;

[EntityName("Unavailability")]
public class Unavailability : ReadModelEntity
{
    public Optional<string> CarId { get; set; }

    public UnavailabilityCausedBy CausedBy { get; set; }

    public Optional<string> CausedByReference { get; set; }

    public Optional<DateTime> From { get; set; }

    public Optional<string> OrganizationId { get; set; }

    public Optional<DateTime> To { get; set; }
}