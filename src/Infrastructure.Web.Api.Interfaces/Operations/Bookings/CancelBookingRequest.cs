namespace Infrastructure.Web.Api.Interfaces.Operations.Bookings;

[Route("/bookings/{id}", ServiceOperation.Delete)]
public class CancelBookingRequest : TenantedDeleteRequest
{
    public required string Id { get; set; }
}