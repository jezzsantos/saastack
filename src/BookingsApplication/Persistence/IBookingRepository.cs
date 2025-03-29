using Application.Interfaces;
using Application.Persistence.Interfaces;
using BookingsApplication.Persistence.ReadModels;
using BookingsDomain;
using Common;
using Domain.Common.ValueObjects;

namespace BookingsApplication.Persistence;

public interface IBookingRepository : IApplicationRepository
{
    Task<Result<Error>> DeleteBookingAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken);

    Task<Result<BookingRoot, Error>> LoadAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken);

    Task<Result<BookingRoot, Error>> SaveAsync(BookingRoot booking, bool reload, CancellationToken cancellationToken);

    Task<Result<BookingRoot, Error>> SaveAsync(BookingRoot booking, CancellationToken cancellationToken);

    Task<Result<QueryResults<Booking>, Error>> SearchAllBookingsAsync(Identifier organizationId, DateTime from,
        DateTime to, SearchOptions searchOptions, CancellationToken cancellationToken);
}