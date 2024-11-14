using Application.Interfaces;
using Common;
using Audit = Application.Resources.Shared.Audit;

namespace AncillaryApplication;

partial interface IAncillaryApplication
{
    Task<Result<bool, Error>> DeliverAuditAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken);

#if TESTINGONLY
    Task<Result<Error>> DrainAllAuditsAsync(ICallerContext caller, CancellationToken cancellationToken);
#endif

#if TESTINGONLY
    Task<Result<SearchResults<Audit>, Error>> SearchAllAuditsAsync(ICallerContext caller, string organizationId,
        SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken);
#endif
}