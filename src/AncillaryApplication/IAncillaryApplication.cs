using Application.Interfaces;
using Common;
using Audit = Application.Resources.Shared.Audit;

namespace AncillaryApplication;

public interface IAncillaryApplication
{
    Task<Result<bool, Error>> DeliverAuditAsync(ICallerContext context, string messageAsJson,
        CancellationToken cancellationToken);

    Task<Result<bool, Error>> DeliverEmailAsync(ICallerContext context, string messageAsJson,
        CancellationToken cancellationToken);

    Task<Result<bool, Error>> DeliverUsageAsync(ICallerContext context, string messageAsJson,
        CancellationToken cancellationToken);

#if TESTINGONLY
    Task<Result<Error>> DrainAllAuditsAsync(ICallerContext context, CancellationToken cancellationToken);
#endif

#if TESTINGONLY
    Task<Result<Error>> DrainAllEmailsAsync(ICallerContext context, CancellationToken cancellationToken);
#endif

#if TESTINGONLY
    Task<Result<Error>> DrainAllUsagesAsync(ICallerContext context, CancellationToken cancellationToken);
#endif

#if TESTINGONLY
    Task<Result<SearchResults<Audit>, Error>> SearchAllAuditsAsync(ICallerContext context, string organizationId,
        SearchOptions searchOptions, GetOptions getOptions, CancellationToken cancellationToken);
#endif
}