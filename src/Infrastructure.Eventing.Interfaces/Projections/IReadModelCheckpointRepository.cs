using Application.Persistence.Interfaces;
using Common;

namespace Infrastructure.Eventing.Interfaces.Projections;

public interface IReadModelCheckpointRepository : IApplicationRepository
{
    Task<Result<int, Error>> LoadCheckpointAsync(string streamName, CancellationToken cancellationToken);

    Task<Result<Error>> SaveCheckpointAsync(string streamName, int position, CancellationToken cancellationToken);
}