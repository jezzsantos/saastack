using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using BookingsApplication.Persistence;
using BookingsApplication.Persistence.ReadModels;
using BookingsDomain;
using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace BookingsInfrastructure.Persistence;

public class BookingRepository : IBookingRepository
{
    private readonly ISnapshottingQueryStore<Booking> _bookingQueries;
    private readonly ISnapshottingDddCommandStore<BookingRoot> _bookings;

    public BookingRepository(IRecorder recorder, ISnapshottingDddCommandStore<BookingRoot> bookingsStore,
        IDomainFactory domainFactory, IDataStore store)
    {
        _bookings = bookingsStore;
        _bookingQueries = new SnapshottingQueryStore<Booking>(recorder, domainFactory, store);
    }

    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await _bookings.DestroyAllAsync(cancellationToken);
    }

    public async Task<Result<Error>> DeleteBookingAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _bookings.GetAsync(id, true, false, cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        if (retrieved.Value.Value.OrganizationId != organizationId)
        {
            return Error.EntityNotFound();
        }

        var deleted = await _bookings.DeleteAsync(id, false, cancellationToken);
        return !deleted.IsSuccessful
            ? deleted.Error
            : Result.Ok;
    }

    public async Task<Result<BookingRoot, Error>> LoadAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _bookings.GetAsync(id, true, false, cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        return retrieved.Value.Value;
    }

    public async Task<Result<BookingRoot, Error>> SaveAsync(BookingRoot booking, bool reload,
        CancellationToken cancellationToken)
    {
        var upserted = await _bookings.UpsertAsync(booking, false, cancellationToken);
        if (!upserted.IsSuccessful)
        {
            return upserted.Error;
        }

        return upserted.Value;
    }

    public async Task<Result<BookingRoot, Error>> SaveAsync(BookingRoot booking, CancellationToken cancellationToken)
    {
        return await SaveAsync(booking, false, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<Booking>, Error>> SearchAllBookingsAsync(Identifier organizationId,
        DateTime from, DateTime to, SearchOptions searchOptions,
        CancellationToken cancellationToken)
    {
        var bookings = await _bookingQueries.QueryAsync(Query.From<Booking>()
            .Where<string>(u => u.OrganizationId, ConditionOperator.EqualTo, organizationId)
            .AndWhere<DateTime>(u => u.Start, ConditionOperator.GreaterThanEqualTo, from)
            .AndWhere<DateTime>(u => u.End, ConditionOperator.LessThanEqualTo, to)
            .WithSearchOptions(searchOptions), cancellationToken: cancellationToken);
        if (!bookings.IsSuccessful)
        {
            return bookings.Error;
        }

        return bookings.Value.Results;
    }
}