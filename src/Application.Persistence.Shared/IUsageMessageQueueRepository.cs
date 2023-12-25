using Application.Persistence.Interfaces;
using Common;

namespace Application.Persistence.Shared;

public interface IUsageMessageQueueRepository : IMessageQueueStore<UsageMessage>, IApplicationRepository
{
    new Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken);
}