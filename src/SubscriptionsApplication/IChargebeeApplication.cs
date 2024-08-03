using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace SubscriptionsApplication;

public interface IChargebeeApplication
{
    Task<Result<Error>> NotifyWebhookEvent(ICallerContext caller, string eventId, string eventType,
        ChargebeeEventContent content, CancellationToken cancellationToken);
}