using AncillaryApplication;
using Application.Resources.Shared;
using Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace AncillaryInfrastructure.Api.Smses;

public sealed class SmsesApi : IWebApiService
{
    private readonly IAncillaryApplication _ancillaryApplication;
    private readonly ICallerContextFactory _callerFactory;

    public SmsesApi(ICallerContextFactory callerFactory, IAncillaryApplication ancillaryApplication)
    {
        _callerFactory = callerFactory;
        _ancillaryApplication = ancillaryApplication;
    }

#if TESTINGONLY
    public async Task<ApiEmptyResult> ConfirmSmsDelivered(ConfirmSmsDeliveredRequest request,
        CancellationToken cancellationToken)
    {
        var delivered = await _ancillaryApplication.ConfirmSmsDeliveredAsync(_callerFactory.Create(),
            request.ReceiptId!, request.DeliveredAtUtc ?? DateTime.UtcNow, cancellationToken);

        return () => delivered.Match(() => new EmptyResponse(),
            error => new Result<EmptyResponse, Error>(error));
    }
#endif

#if TESTINGONLY
    public async Task<ApiEmptyResult> ConfirmSmsDeliveryFailed(ConfirmSmsDeliveryFailedRequest request,
        CancellationToken cancellationToken)
    {
        var delivered = await _ancillaryApplication.ConfirmSmsDeliveryFailedAsync(_callerFactory.Create(),
            request.ReceiptId!, request.FailedAtUtc ?? DateTime.UtcNow, request.Reason ?? "none", cancellationToken);

        return () => delivered.Match(() => new EmptyResponse(),
            error => new Result<EmptyResponse, Error>(error));
    }
#endif

#if TESTINGONLY
    public async Task<ApiEmptyResult> DrainAll(DrainAllSmsesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ancillaryApplication.DrainAllSmsesAsync(_callerFactory.Create(), cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }
#endif

    public async Task<ApiSearchResult<DeliveredSms, SearchSmsDeliveriesResponse>> SearchAll(
        SearchSmsDeliveriesRequest request, CancellationToken cancellationToken)
    {
        var deliveries = await _ancillaryApplication.SearchAllSmsDeliveriesAsync(_callerFactory.Create(),
            request.SinceUtc, request.Tags, request.ToSearchOptions(), request.ToGetOptions(), cancellationToken);

        return () =>
            deliveries.HandleApplicationResult(c => new SearchSmsDeliveriesResponse
                { Smses = c.Results, Metadata = c.Metadata });
    }

    public async Task<ApiPostResult<bool, DeliverMessageResponse>> Send(SendSmsRequest request,
        CancellationToken cancellationToken)
    {
        var sent =
            await _ancillaryApplication.SendSmsAsync(_callerFactory.Create(), request.Message!, cancellationToken);

        return () => sent.HandleApplicationResult<bool, DeliverMessageResponse>(st =>
            new PostResult<DeliverMessageResponse>(new DeliverMessageResponse { IsSent = st }));
    }
}