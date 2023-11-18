using System.Reflection;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;

namespace Domain.Interfaces;

/// <summary>
///     Defines a factory for rehydrating domain objects for persistence
/// </summary>
public interface IDomainFactory
{
    /// <summary>
    ///     Registers all the aggregate roots, entities and value objects from the specified
    ///     <see cref="assembliesContainingFactories" />
    /// </summary>
    void RegisterDomainTypesFromAssemblies(params Assembly[] assembliesContainingFactories);

    /// <summary>
    ///     Rehydrates the registered <see cref="aggregateType" /> using the specified <see cref="rehydratingProperties" />
    ///     using the <see cref="AggregateRootFactory{TAggregateRoot}" />
    /// </summary>
    IDehydratableAggregateRoot RehydrateAggregateRoot(Type aggregateType, HydrationProperties rehydratingProperties);

    /// <summary>
    ///     Rehydrates the registered <see cref="entityType" /> using the specified <see cref="rehydratingProperties" />
    ///     using the <see cref="EntityFactory{TEntity}" />
    /// </summary>
    IDehydratableEntity RehydrateEntity(Type entityType, HydrationProperties rehydratingProperties);

    /// <summary>
    ///     Rehydrates the registered <see cref="valueObjectType" /> using the specified
    ///     <see cref="rehydratingPropertyValue" />
    ///     using the <see cref="ValueObjectFactory{TValueObject}" />
    /// </summary>
    IDehydratableValueObject RehydrateValueObject(Type valueObjectType, string rehydratingPropertyValue);
}