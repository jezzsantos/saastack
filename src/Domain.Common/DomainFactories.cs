using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;

namespace Domain.Common;

/// <summary>
///     Creates a new instance of an <see cref="TAggregateRoot" /> given the <see cref="Identifier" /> and
///     <see cref="rehydratingProperties" />
/// </summary>
public delegate TAggregateRoot AggregateRootFactory<out TAggregateRoot>(Identifier identifier,
    IDependencyContainer container, IReadOnlyDictionary<string, object?> rehydratingProperties)
    where TAggregateRoot : IEventSourcedAggregateRoot;

/// <summary>
///     Creates a new instance of an <see cref="TEntity" /> given the <see cref="Identifier" /> and
///     <see cref="rehydratingProperties" />
/// </summary>
public delegate TEntity EntityFactory<out TEntity>(Identifier identifier,
    IDependencyContainer container, IReadOnlyDictionary<string, object?> rehydratingProperties)
    where TEntity : IDehydratableEntity;

/// <summary>
///     Creates a new instance of a <see cref="TValueObject" /> given a <see cref="hydratingProperty" />
/// </summary>
public delegate TValueObject ValueObjectFactory<out TValueObject>(string hydratingProperty,
    IDependencyContainer container)
    where TValueObject : IPersistableValueObject;