using Application.Persistence.Interfaces;
using Common;

namespace Infrastructure.Eventing.Interfaces.Projections;

/// <summary>
///     Defines a repository for checkpoints used in projections
/// </summary>
public interface IProjectionCheckpointRepository : IApplicationRepository
{
    Task<Result<int, Error>> LoadCheckpointAsync(string streamName, CancellationToken cancellationToken);

    Task<Result<Error>> SaveCheckpointAsync(string streamName, int position, CancellationToken cancellationToken);
}