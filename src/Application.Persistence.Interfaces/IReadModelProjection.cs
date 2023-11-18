using Common;
using Domain.Interfaces.Entities;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a projection that handles new raised events from the specified <see cref="RootAggregateType" />
/// </summary>
public interface IReadModelProjection
{
    /// <summary>
    ///     Returns the type of the root aggregate that produces the events
    /// </summary>
    Type RootAggregateType { get; }

    /// <summary>
    ///     Handles the projection of a new <see cref="changeEvent" />, and reports whether that specific event was handled or
    ///     not
    /// </summary>
    Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent, CancellationToken cancellationToken);
}