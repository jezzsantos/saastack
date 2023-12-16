using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Bookings;

[Route("/bookings/{id}", ServiceOperation.Delete)]
public class CancelBookingRequest : TenantedDeleteRequest
{
    public required string Id { get; set; }
}