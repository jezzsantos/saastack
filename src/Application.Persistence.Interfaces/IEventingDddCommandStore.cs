using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a store for reading/writing individual DDD Aggregate by [CQRS] commands, that use event sourcing
/// </summary>
public interface IEventingDddCommandStore<TAggregateRoot> : IEventNotifyingStore
    where TAggregateRoot : IEventSourcedAggregateRoot
{
    /// <summary>
    ///     Permanently destroys all aggregates in the store
    /// </summary>
    Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Loads the existing aggregate, by left-folding the stream of events in the store
    /// </summary>
    Task<Result<TAggregateRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken);

    /// <summary>
    ///     Saves the existing aggregate, by adding any new events to the existing stream of events in the store
    /// </summary>
    Task<Result<Error>> SaveAsync(TAggregateRoot aggregate, CancellationToken cancellationToken);
}