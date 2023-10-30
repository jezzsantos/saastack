namespace BookingsDomain;

public static class Validations
{
    public static class Booking
    {
        public static readonly TimeSpan MaximumBookingDuration = TimeSpan.FromHours(8);
        public static readonly TimeSpan MinimumBookingDuration = TimeSpan.FromMinutes(15);
    }
}