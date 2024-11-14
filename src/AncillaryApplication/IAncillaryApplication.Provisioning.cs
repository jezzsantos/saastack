using Application.Interfaces;
using Common;

namespace AncillaryApplication;

partial interface IAncillaryApplication
{
#if TESTINGONLY
    Task<Result<Error>> DrainAllProvisioningsAsync(ICallerContext caller, CancellationToken cancellationToken);
#endif

    Task<Result<bool, Error>> NotifyProvisioningAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken);
}