using Application.Persistence.Common;
using QueryAny;

namespace BookingsApplication.Persistence.ReadModels;

[EntityName("Booking")]
public class Booking : ReadModelEntity
{
    public required string BorrowerId { get; set; }

    public required string CarId { get; set; }

    public required DateTime End { get; set; }

    public required string OrganisationId { get; set; }

    public required DateTime Start { get; set; }
}