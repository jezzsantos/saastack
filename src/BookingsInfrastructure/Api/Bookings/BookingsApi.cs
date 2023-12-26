using Application.Interfaces;
using Application.Resources.Shared;
using BookingsApplication;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Bookings;

namespace BookingsInfrastructure.Api.Bookings;

public sealed class BookingsApi : IWebApiService
{
    private const string OrganizationId = "org_01234567890123456789012"; //TODO: get this from tenancy
    private readonly IBookingsApplication _bookingsApplication;
    private readonly ICallerContext _context;

    public BookingsApi(ICallerContext context, IBookingsApplication bookingsApplication)
    {
        _context = context;
        _bookingsApplication = bookingsApplication;
    }

    public async Task<ApiDeleteResult> Cancel(CancelBookingRequest request, CancellationToken cancellationToken)
    {
        var booking =
            await _bookingsApplication.CancelBookingAsync(_context, OrganizationId, request.Id, cancellationToken);
        return () => booking.HandleApplicationResult();
    }

    public async Task<ApiPostResult<Booking, MakeBookingResponse>> Make(MakeBookingRequest request,
        CancellationToken cancellationToken)
    {
        var booking = await _bookingsApplication.MakeBookingAsync(_context, OrganizationId, request.CarId,
            request.StartUtc, request.EndUtc, cancellationToken);

        return () => booking.HandleApplicationResult<MakeBookingResponse, Booking>(c =>
            new PostResult<MakeBookingResponse>(new MakeBookingResponse { Booking = c }));
    }

    public async Task<ApiSearchResult<Booking, SearchAllBookingsResponse>> SearchAll(SearchAllBookingsRequest request,
        CancellationToken cancellationToken)
    {
        var bookings = await _bookingsApplication.SearchAllBookingsAsync(_context, OrganizationId, request.FromUtc,
            request.ToUtc, request.ToSearchOptions(), request.ToGetOptions(), cancellationToken);

        return () =>
            bookings.HandleApplicationResult(c => new SearchAllBookingsResponse
                { Bookings = c.Results, Metadata = c.Metadata });
    }
}