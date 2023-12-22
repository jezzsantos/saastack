using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Common;

namespace Application.Services.Shared;

public interface IUsageMessageQueueRepository : IMessageQueueStore<UsageMessage>, IApplicationRepository
{
    new Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken);
}