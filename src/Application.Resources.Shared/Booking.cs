using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class Booking : IIdentifiableResource
{
    public required string BorrowerId { get; set; }

    public required string CarId { get; set; }

    public required DateTime EndUtc { get; set; }

    public required DateTime StartUtc { get; set; }

    public required string Id { get; set; }
}