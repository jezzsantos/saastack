using AncillaryApplication;
using Application.Resources.Shared;
using Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace AncillaryInfrastructure.Api.Emails;

public sealed class EmailsApi : IWebApiService
{
    private readonly IAncillaryApplication _ancillaryApplication;
    private readonly ICallerContextFactory _callerFactory;

    public EmailsApi(ICallerContextFactory callerFactory, IAncillaryApplication ancillaryApplication)
    {
        _callerFactory = callerFactory;
        _ancillaryApplication = ancillaryApplication;
    }

#if TESTINGONLY
    public async Task<ApiEmptyResult> ConfirmEmailDelivered(ConfirmEmailDeliveredRequest request,
        CancellationToken cancellationToken)
    {
        var delivered = await _ancillaryApplication.ConfirmEmailDeliveredAsync(_callerFactory.Create(),
            request.ReceiptId!, request.DeliveredAtUtc ?? DateTime.UtcNow, cancellationToken);

        return () => delivered.Match(() => new EmptyResponse(),
            error => new Result<EmptyResponse, Error>(error));
    }
#endif

#if TESTINGONLY
    public async Task<ApiEmptyResult> ConfirmEmailDeliveryFailed(ConfirmEmailDeliveryFailedRequest request,
        CancellationToken cancellationToken)
    {
        var delivered = await _ancillaryApplication.ConfirmEmailDeliveryFailedAsync(_callerFactory.Create(),
            request.ReceiptId!, request.FailedAtUtc ?? DateTime.UtcNow, request.Reason ?? "none", cancellationToken);

        return () => delivered.Match(() => new EmptyResponse(),
            error => new Result<EmptyResponse, Error>(error));
    }
#endif

#if TESTINGONLY
    public async Task<ApiEmptyResult> DrainAll(DrainAllEmailsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ancillaryApplication.DrainAllEmailsAsync(_callerFactory.Create(), cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }
#endif

    public async Task<ApiSearchResult<DeliveredEmail, SearchEmailDeliveriesResponse>> SearchAll(
        SearchEmailDeliveriesRequest request, CancellationToken cancellationToken)
    {
        var deliveries = await _ancillaryApplication.SearchAllEmailDeliveriesAsync(_callerFactory.Create(),
            request.SinceUtc, request.Tags, request.ToSearchOptions(), request.ToGetOptions(), cancellationToken);

        return () =>
            deliveries.HandleApplicationResult(c => new SearchEmailDeliveriesResponse
                { Emails = c.Results, Metadata = c.Metadata });
    }

    public async Task<ApiPostResult<bool, DeliverMessageResponse>> Send(SendEmailRequest request,
        CancellationToken cancellationToken)
    {
        var sent =
            await _ancillaryApplication.SendEmailAsync(_callerFactory.Create(), request.Message!, cancellationToken);

        return () => sent.HandleApplicationResult<bool, DeliverMessageResponse>(st =>
            new PostResult<DeliverMessageResponse>(new DeliverMessageResponse { IsSent = st }));
    }
}