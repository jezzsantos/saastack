using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Bookings;

public class MakeBookingResponse : IWebResponse
{
    public required Booking Booking { get; set; }
}