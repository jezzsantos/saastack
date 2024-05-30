using Application.Persistence.Interfaces;
using Common;

namespace Application.Services.Shared;

public interface IDomainEventConsumerService
{
    Task<Result<Error>> NotifyAsync(EventStreamChangeEvent changeEvent, CancellationToken cancellationToken);
}