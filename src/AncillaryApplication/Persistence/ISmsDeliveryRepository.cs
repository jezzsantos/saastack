using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Interfaces;
using Application.Persistence.Interfaces;
using Common;

namespace AncillaryApplication.Persistence;

public interface ISmsDeliveryRepository : IApplicationRepository
{
    Task<Result<Optional<SmsDeliveryRoot>, Error>> FindByMessageIdAsync(QueuedMessageId messageId,
        CancellationToken cancellationToken);

    Task<Result<Optional<SmsDeliveryRoot>, Error>> FindByReceiptIdAsync(string receiptId,
        CancellationToken cancellationToken);

    Task<Result<SmsDeliveryRoot, Error>> SaveAsync(SmsDeliveryRoot delivery, bool reload,
        CancellationToken cancellationToken);

    Task<Result<List<SmsDelivery>, Error>> SearchAllDeliveriesAsync(DateTime? sinceUtc, IReadOnlyList<string>? tags,
        SearchOptions searchOptions, CancellationToken cancellationToken);
}