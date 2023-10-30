using Application.Interfaces.Resources;

namespace Infrastructure.Web.Api.Interfaces.Operations.Bookings;

public class SearchAllBookingsResponse : SearchResponse
{
    public List<Booking>? Bookings { get; set; }
}