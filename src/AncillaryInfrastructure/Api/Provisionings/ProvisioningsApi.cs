using AncillaryApplication;
using Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace AncillaryInfrastructure.Api.Provisionings;

public sealed class ProvisioningsApi : IWebApiService
{
    private readonly IAncillaryApplication _ancillaryApplication;
    private readonly ICallerContextFactory _callerFactory;

    public ProvisioningsApi(ICallerContextFactory callerFactory, IAncillaryApplication ancillaryApplication)
    {
        _callerFactory = callerFactory;
        _ancillaryApplication = ancillaryApplication;
    }

#if TESTINGONLY
    public async Task<ApiEmptyResult> DrainAll(DrainAllProvisioningsRequest request,
        CancellationToken cancellationToken)
    {
        var result =
            await _ancillaryApplication.DrainAllProvisioningsAsync(_callerFactory.Create(), cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }
#endif

    public async Task<ApiPostResult<bool, DeliverMessageResponse>> Notify(NotifyProvisioningRequest request,
        CancellationToken cancellationToken)
    {
        var delivered =
            await _ancillaryApplication.NotifyProvisioningAsync(_callerFactory.Create(), request.Message,
                cancellationToken);

        return () => delivered.HandleApplicationResult<bool, DeliverMessageResponse>(_ =>
            new PostResult<DeliverMessageResponse>(new DeliverMessageResponse { IsDelivered = true }));
    }
}