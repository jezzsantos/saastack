using AncillaryApplication.Persistence.ReadModels;
using AncillaryDomain;
using Application.Interfaces;
using Application.Persistence.Interfaces;
using Common;

namespace AncillaryApplication.Persistence;

public interface IEmailDeliveryRepository : IApplicationRepository
{
    Task<Result<Optional<EmailDeliveryRoot>, Error>> FindByMessageIdAsync(QueuedMessageId messageId,
        CancellationToken cancellationToken);

    Task<Result<Optional<EmailDeliveryRoot>, Error>> FindByReceiptIdAsync(string receiptId,
        CancellationToken cancellationToken);

    Task<Result<EmailDeliveryRoot, Error>> SaveAsync(EmailDeliveryRoot delivery, bool reload,
        CancellationToken cancellationToken);

    Task<Result<QueryResults<EmailDelivery>, Error>> SearchAllAsync(DateTime? sinceUtc, string? organizationId,
        IReadOnlyList<string>? tags, SearchOptions searchOptions, CancellationToken cancellationToken);
}