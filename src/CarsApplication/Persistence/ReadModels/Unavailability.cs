using Application.Persistence.Common;
using CarsDomain;
using QueryAny;

namespace CarsApplication.Persistence.ReadModels;

[EntityName("Unavailability")]
public class Unavailability : ReadModelEntity
{
    public required string CarId { get; set; }

    public UnavailabilityCausedBy CausedBy { get; set; }

    public string? CausedByReference { get; set; }

    public DateTime From { get; set; }

    public required string OrganisationId { get; set; }

    public DateTime To { get; set; }
}