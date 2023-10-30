namespace Infrastructure.Web.Api.Interfaces.Operations.Bookings;

[Route("/bookings", ServiceOperation.Search)]
public class SearchAllBookingsRequest : TenantedSearchRequest<SearchAllBookingsResponse>
{
    public DateTime? FromUtc { get; set; }

    public DateTime? ToUtc { get; set; }
}