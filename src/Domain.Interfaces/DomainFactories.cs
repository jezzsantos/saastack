using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using Domain.Interfaces.ValueObjects;

namespace Domain.Interfaces;

/// <summary>
///     Creates a new instance of an <see cref="TAggregateRoot" /> given the <see cref="identifier" /> and
///     <see cref="rehydratingProperties" />
/// </summary>
public delegate TAggregateRoot AggregateRootFactory<out TAggregateRoot>(ISingleValueObject<string> identifier,
    IDependencyContainer container, HydrationProperties rehydratingProperties)
    where TAggregateRoot : IDehydratableAggregateRoot;

/// <summary>
///     Creates a new instance of an <see cref="TEntity" /> given the <see cref="identifier" /> and
///     <see cref="rehydratingProperties" />
/// </summary>
public delegate TEntity EntityFactory<out TEntity>(ISingleValueObject<string> identifier,
    IDependencyContainer container, HydrationProperties rehydratingProperties)
    where TEntity : IDehydratableEntity;

/// <summary>
///     Creates a new instance of a <see cref="TValueObject" /> given a <see cref="hydratingProperty" />
/// </summary>
public delegate TValueObject ValueObjectFactory<out TValueObject>(string hydratingProperty,
    IDependencyContainer container)
    where TValueObject : IDehydratableValueObject;