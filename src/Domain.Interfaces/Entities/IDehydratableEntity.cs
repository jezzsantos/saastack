using QueryAny;

namespace Domain.Interfaces.Entities;

/// <summary>
///     Defines an entity that can persist its state to a set of properties
/// </summary>
public interface IDehydratableEntity : IIdentifiableEntity, IQueryableEntity
{
    bool? IsDeleted { get; }

    DateTime? LastPersistedAtUtc { get; }

    Dictionary<string, object?> Dehydrate();
}