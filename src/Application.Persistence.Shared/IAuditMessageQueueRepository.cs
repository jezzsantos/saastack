using Application.Persistence.Interfaces;
using Common;

namespace Application.Persistence.Shared;

public interface IAuditMessageQueueRepository : IMessageQueueStore<AuditMessage>, IApplicationRepository
{
    new Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken);
}