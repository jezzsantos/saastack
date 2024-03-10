namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines a root aggregate that can persist its state to a set of properties
/// </summary>
public interface IDehydratableAggregateRoot : IDehydratableEntity;