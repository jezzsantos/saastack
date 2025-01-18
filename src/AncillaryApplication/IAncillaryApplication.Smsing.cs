using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace AncillaryApplication;

partial interface IAncillaryApplication
{
    Task<Result<Error>> ConfirmSmsDeliveredAsync(ICallerContext caller, string receiptId, DateTime deliveredAt,
        CancellationToken cancellationToken);

    Task<Result<Error>> ConfirmSmsDeliveryFailedAsync(ICallerContext caller, string receiptId, DateTime failedAt,
        string reason,
        CancellationToken cancellationToken);

#if TESTINGONLY
    Task<Result<Error>> DrainAllSmsesAsync(ICallerContext caller, CancellationToken cancellationToken);
#endif

    Task<Result<SearchResults<DeliveredSms>, Error>> SearchAllSmsDeliveriesAsync(ICallerContext caller,
        DateTime? sinceUtc, string? organizationId, IReadOnlyList<string>? tags, SearchOptions searchOptions,
        GetOptions getOptions,
        CancellationToken cancellationToken);

    Task<Result<bool, Error>> SendSmsAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken);
}