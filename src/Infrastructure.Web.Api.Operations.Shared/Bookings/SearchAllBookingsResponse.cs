using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Bookings;

public class SearchAllBookingsResponse : SearchResponse
{
    public List<Booking>? Bookings { get; set; }
}