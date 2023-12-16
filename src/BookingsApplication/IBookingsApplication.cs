using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace BookingsApplication;

public interface IBookingsApplication
{
    Task<Result<Error>> CancelBookingAsync(ICallerContext caller, string organizationId, string id,
        CancellationToken cancellationToken);

    Task<Result<Booking, Error>> MakeBookingAsync(ICallerContext caller, string organizationId, string carId,
        DateTime startUtc, DateTime? endUtc, CancellationToken cancellationToken);

    Task<Result<SearchResults<Booking>, Error>> SearchAllBookingsAsync(ICallerContext caller, string organizationId,
        DateTime? fromUtc, DateTime? toUtc, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken);
}