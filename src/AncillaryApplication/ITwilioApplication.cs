using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace AncillaryApplication;

public interface ITwilioApplication
{
    Task<Result<Error>> NotifyWebhookEvent(ICallerContext caller, TwilioEventData eventData,
        CancellationToken cancellationToken);
}