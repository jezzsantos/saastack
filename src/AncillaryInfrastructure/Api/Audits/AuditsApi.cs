using AncillaryApplication;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Ancillary;

namespace AncillaryInfrastructure.Api.Audits;

public class AuditsApi : IWebApiService
{
    private readonly IAncillaryApplication _ancillaryApplication;
    private readonly ICallerContext _context;

    public AuditsApi(ICallerContext context, IAncillaryApplication ancillaryApplication)
    {
        _context = context;
        _ancillaryApplication = ancillaryApplication;
    }

    public async Task<ApiPostResult<bool, DeliverMessageResponse>> Deliver(DeliverAuditRequest request,
        CancellationToken cancellationToken)
    {
        var delivered = await _ancillaryApplication.DeliverAuditAsync(_context, request.Message, cancellationToken);

        return () => delivered.HandleApplicationResult<DeliverMessageResponse, bool>(_ =>
            new PostResult<DeliverMessageResponse>(new DeliverMessageResponse { IsDelivered = true }));
    }

#if TESTINGONLY
         public async Task<ApiEmptyResult> DrainAll(DrainAllAuditsRequest request,
        CancellationToken cancellationToken)
    {
        await _ancillaryApplication.DrainAllAuditsAsync(_context, cancellationToken);

        return () => new Result<EmptyResponse, Error>();
    }
#endif

#if TESTINGONLY
         public async Task<ApiSearchResult<Audit, SearchAllAuditsResponse>> SearchAll(
        SearchAllAuditsRequest request, CancellationToken cancellationToken)
    {
        var audits = await _ancillaryApplication.SearchAllAuditsAsync(_context,
            request.OrganizationId!, request.ToSearchOptions(), request.ToGetOptions(), cancellationToken);

        return () => audits.HandleApplicationResult(a => new SearchAllAuditsResponse
            { Audits = a.Results, Metadata = a.Metadata });
    }
#endif
}