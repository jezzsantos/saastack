using Application.Interfaces;
using BookingsApplication.Persistence;
using BookingsApplication.Persistence.ReadModels;
using BookingsDomain;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;

namespace BookingsInfrastructure.Persistence;

//TODO: stop using this in-memory repo and move to persistence layer
public class BookingRepository : IBookingRepository
{
    private static readonly List<BookingRoot> Bookings = new();

    public async Task<Result<Error>> DestroyAllAsync()
    {
        await Task.CompletedTask;

        Bookings.Clear();

        return Result.Ok;
    }

    public async Task<Result<Error>> DeleteBookingAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        var booking = Bookings.Find(root => root.Id == id);
        if (booking.NotExists())
        {
            return Error.EntityNotFound();
        }

        Bookings.Remove(booking);

        return Result.Ok;
    }

    public async Task<Result<BookingRoot, Error>> LoadAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        var booking = Bookings.Find(root => root.Id == id);
        if (booking.NotExists())
        {
            return Error.EntityNotFound();
        }

        return booking;
    }

    public async Task<Result<BookingRoot, Error>> SaveAsync(BookingRoot booking, bool reload,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        var existing = Bookings.Find(root => root.Id == booking.Id);
        if (existing.NotExists())
        {
            Bookings.Add(booking);
            return booking;
        }

        existing = booking;
        return existing;
    }

    public async Task<Result<BookingRoot, Error>> SaveAsync(BookingRoot booking, CancellationToken cancellationToken)
    {
        return await SaveAsync(booking, false, cancellationToken);
    }

    public async Task<Result<IReadOnlyList<Booking>, Error>> SearchAllBookingsAsync(Identifier organizationId,
        DateTime from, DateTime to, SearchOptions searchOptions,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        return Bookings
            .FindAll(booking => booking.Start >= from && booking.End <= to)
            .Select(booking => booking.ToBooking())
            .ToList();
    }
}

internal static class RepositoryExtensions
{
    public static Booking ToBooking(this BookingRoot booking)
    {
        return new Booking
        {
            Id = booking.Id,
            OrganisationId = booking.OrganizationId,
            BorrowerId = booking.BorrowerId!,
            CarId = booking.CarId!,
            Start = booking.Start!.Value,
            End = booking.End!.Value
        };
    }
}