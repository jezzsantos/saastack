using Application.Resources.Shared;
using BookingsApplication;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Bookings;

namespace BookingsInfrastructure.Api.Bookings;

public sealed class BookingsApi : IWebApiService
{
    private readonly IBookingsApplication _bookingsApplication;
    private readonly ICallerContextFactory _callerFactory;

    public BookingsApi(ICallerContextFactory callerFactory, IBookingsApplication bookingsApplication)
    {
        _callerFactory = callerFactory;
        _bookingsApplication = bookingsApplication;
    }

    public async Task<ApiDeleteResult> Cancel(CancelBookingRequest request, CancellationToken cancellationToken)
    {
        var booking =
            await _bookingsApplication.CancelBookingAsync(_callerFactory.Create(), request.OrganizationId!, request.Id,
                cancellationToken);
        return () => booking.HandleApplicationResult();
    }

    public async Task<ApiPostResult<Booking, MakeBookingResponse>> Make(MakeBookingRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingsApplication.MakeBookingAsync(_callerFactory.Create(), request.OrganizationId!,
            request.CarId, request.StartUtc, request.EndUtc, cancellationToken);

        return () => booking.HandleApplicationResult<Booking, MakeBookingResponse>(c =>
            new PostResult<MakeBookingResponse>(new MakeBookingResponse { Booking = c }));
    }

    public async Task<ApiSearchResult<Booking, SearchAllBookingsResponse>> SearchAll(SearchAllBookingsRequest request,
        CancellationToken cancellationToken)
    {
        var bookings = await _bookingsApplication.SearchAllBookingsAsync(_callerFactory.Create(),
            request.OrganizationId!, request.FromUtc, request.ToUtc, request.ToSearchOptions(), request.ToGetOptions(),
            cancellationToken);

        return () =>
            bookings.HandleApplicationResult(c => new SearchAllBookingsResponse
                { Bookings = c.Results, Metadata = c.Metadata });
    }
}