using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Bookings;

/// <summary>
///     Makes a new booking for a specific car
/// </summary>
[Route("/bookings", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class MakeBookingRequest : TenantedRequest<MakeBookingResponse>
{
    [Required] public string? CarId { get; set; }

    public DateTime? EndUtc { get; set; }

    [Required] public DateTime? StartUtc { get; set; }
}