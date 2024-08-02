using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace AncillaryApplication;

public interface IMailgunApplication
{
    Task<Result<Error>> NotifyWebhookEvent(ICallerContext caller, MailgunEventData eventData,
        CancellationToken cancellationToken);
}