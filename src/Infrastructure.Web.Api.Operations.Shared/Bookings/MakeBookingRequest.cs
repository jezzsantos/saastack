using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Bookings;

[Route("/bookings", ServiceOperation.Post)]
public class MakeBookingRequest : TenantedRequest<MakeBookingResponse>
{
    public required string CarId { get; set; }

    public DateTime? EndUtc { get; set; }

    public required DateTime StartUtc { get; set; }
}