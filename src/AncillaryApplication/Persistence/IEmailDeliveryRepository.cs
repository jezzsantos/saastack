using Application.Persistence.Interfaces;
using Common;

namespace AncillaryApplication.Persistence;

public interface IEmailDeliveryRepository: IApplicationRepository
{
    Task<Result<Optional<EmailDeliveryRoot>, Error>> FindDeliveryByMessageIdAsync(string messageId);

    Task<Result<Error>> SaveAsync(EmailDeliveryRoot delivery, CancellationToken cancellationToken);

}