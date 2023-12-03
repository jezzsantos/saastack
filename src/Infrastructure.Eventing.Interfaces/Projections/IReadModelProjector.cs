using Application.Persistence.Interfaces;
using Common;

namespace Infrastructure.Eventing.Interfaces.Projections;

/// <summary>
///     Defines a projector of events from a source of read model projections
/// </summary>
public interface IReadModelProjector
{
    /// <summary>
    ///     Returns the projection registrations
    /// </summary>
    IReadOnlyList<IReadModelProjection> Projections { get; }

    /// <summary>
    ///     Writes the <see cref="events" /> from the stream
    /// </summary>
    Task<Result<Error>> WriteEventStreamAsync(string streamName, List<EventStreamChangeEvent> events,
        CancellationToken cancellationToken);
}