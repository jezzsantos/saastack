using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

public interface ISubscriptionsService
{
    Task<Result<SubscriptionWithPlan, Error>> GetSubscriptionAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);
}