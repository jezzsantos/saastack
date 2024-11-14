using Application.Interfaces;
using Common;

namespace AncillaryApplication;

partial interface IAncillaryApplication
{
    Task<Result<bool, Error>> DeliverUsageAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken);

#if TESTINGONLY
    Task<Result<Error>> DrainAllUsagesAsync(ICallerContext caller, CancellationToken cancellationToken);
#endif
}