using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Common;

namespace Application.Services.Shared;

public interface IAuditMessageQueueRepository : IMessageQueueStore<AuditMessage>, IApplicationRepository
{
    new Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken);
}