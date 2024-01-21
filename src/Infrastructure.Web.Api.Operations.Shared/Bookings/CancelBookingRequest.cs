using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Bookings;

[Route("/bookings/{id}", ServiceOperation.Delete, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class CancelBookingRequest : TenantedDeleteRequest
{
    public required string Id { get; set; }
}