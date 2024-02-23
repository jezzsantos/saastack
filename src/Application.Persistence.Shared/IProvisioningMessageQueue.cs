using Application.Persistence.Interfaces;
using Application.Persistence.Shared.ReadModels;
using Common;

namespace Application.Persistence.Shared;

public interface IProvisioningMessageQueue : IMessageQueueStore<ProvisioningMessage>, IApplicationRepository
{
    new Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken);
}