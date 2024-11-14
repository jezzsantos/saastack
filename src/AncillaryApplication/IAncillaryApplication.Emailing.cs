using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace AncillaryApplication;

partial interface IAncillaryApplication
{
    Task<Result<Error>> ConfirmEmailDeliveredAsync(ICallerContext caller, string receiptId, DateTime deliveredAt,
        CancellationToken cancellationToken);

    Task<Result<Error>> ConfirmEmailDeliveryFailedAsync(ICallerContext caller, string receiptId, DateTime failedAt,
        string reason,
        CancellationToken cancellationToken);

#if TESTINGONLY
    Task<Result<Error>> DrainAllEmailsAsync(ICallerContext caller, CancellationToken cancellationToken);
#endif

    Task<Result<SearchResults<DeliveredEmail>, Error>> SearchAllEmailDeliveriesAsync(ICallerContext caller,
        DateTime? sinceUtc, IReadOnlyList<string>? tags, SearchOptions searchOptions, GetOptions getOptions,
        CancellationToken cancellationToken);

    Task<Result<bool, Error>> SendEmailAsync(ICallerContext caller, string messageAsJson,
        CancellationToken cancellationToken);
}