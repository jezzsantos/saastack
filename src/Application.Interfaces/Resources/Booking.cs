namespace Application.Interfaces.Resources;

public class Booking : IIdentifiableResource
{
    public required string Id { get; set; }

    public string? BorrowerId { get; set; }

    public string? CarId { get; set; }

    public DateTime? EndUtc { get; set; }

    public DateTime? StartUtc { get; set; }
}