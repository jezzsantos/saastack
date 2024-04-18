using AncillaryApplication;
using Common;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace AncillaryInfrastructure.Api.Usages;

public sealed class UsagesApi : IWebApiService
{
    private readonly IAncillaryApplication _ancillaryApplication;
    private readonly ICallerContextFactory _contextFactory;

    public UsagesApi(ICallerContextFactory contextFactory, IAncillaryApplication ancillaryApplication)
    {
        _contextFactory = contextFactory;
        _ancillaryApplication = ancillaryApplication;
    }

    public async Task<ApiPostResult<bool, DeliverMessageResponse>> Deliver(DeliverUsageRequest request,
        CancellationToken cancellationToken)
    {
        var delivered =
            await _ancillaryApplication.DeliverUsageAsync(_contextFactory.Create(), request.Message, cancellationToken);

        return () => delivered.HandleApplicationResult<bool, DeliverMessageResponse>(_ =>
            new PostResult<DeliverMessageResponse>(new DeliverMessageResponse { IsDelivered = true }));
    }

#if TESTINGONLY
    public async Task<ApiEmptyResult> DrainAll(DrainAllUsagesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ancillaryApplication.DrainAllUsagesAsync(_contextFactory.Create(), cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }
#endif
}