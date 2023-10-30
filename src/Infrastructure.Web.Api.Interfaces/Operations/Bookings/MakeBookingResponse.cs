using Application.Interfaces.Resources;

namespace Infrastructure.Web.Api.Interfaces.Operations.Bookings;

public class MakeBookingResponse : IWebResponse
{
    public Booking? Booking { get; set; }
}