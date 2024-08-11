using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Bookings;

/// <summary>
///     Lists all the bookings for all cars
/// </summary>
[Route("/bookings", OperationMethod.Search, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class SearchAllBookingsRequest : TenantedSearchRequest<SearchAllBookingsRequest, SearchAllBookingsResponse>
{
    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }
}