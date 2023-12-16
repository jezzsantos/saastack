using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Bookings;

[Route("/bookings", ServiceOperation.Search)]
public class SearchAllBookingsRequest : TenantedSearchRequest<SearchAllBookingsResponse>
{
    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }
}