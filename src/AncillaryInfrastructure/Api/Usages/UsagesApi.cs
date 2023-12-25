using AncillaryApplication;
using Application.Interfaces;
using Common;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace AncillaryInfrastructure.Api.Usages;

public class UsagesApi : IWebApiService
{
    private readonly IAncillaryApplication _ancillaryApplication;
    private readonly ICallerContext _context;

    public UsagesApi(ICallerContext context, IAncillaryApplication ancillaryApplication)
    {
        _context = context;
        _ancillaryApplication = ancillaryApplication;
    }

    public async Task<ApiPostResult<bool, DeliverMessageResponse>> Deliver(DeliverUsageRequest request,
        CancellationToken cancellationToken)
    {
        var delivered = await _ancillaryApplication.DeliverUsageAsync(_context, request.Message, cancellationToken);

        return () => delivered.HandleApplicationResult<DeliverMessageResponse, bool>(_ =>
            new PostResult<DeliverMessageResponse>(new DeliverMessageResponse { IsDelivered = true }));
    }

#if TESTINGONLY
    public async Task<ApiEmptyResult> DrainAll(DrainAllUsagesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ancillaryApplication.DrainAllUsagesAsync(_context, cancellationToken);

        return () => result.Match(() => new Result<EmptyResponse, Error>(),
            error => new Result<EmptyResponse, Error>(error));
    }
#endif
}