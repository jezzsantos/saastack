using Application.Persistence.Common;
using Common;
using QueryAny;

namespace BookingsApplication.Persistence.ReadModels;

[EntityName("Booking")]
public class Booking : SnapshottedReadModelEntity
{
    public Optional<string> BorrowerId { get; set; }

    public Optional<string> CarId { get; set; }

    public Optional<DateTime> End { get; set; }

    public Optional<string> OrganizationId { get; set; }

    public Optional<DateTime> Start { get; set; }
}