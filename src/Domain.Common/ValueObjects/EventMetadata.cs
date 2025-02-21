using Domain.Interfaces;
using JetBrains.Annotations;

namespace Domain.Common.ValueObjects;

/// <summary>
///     Provides the metadata describing an event
/// </summary>
public sealed class EventMetadata : SingleValueObjectBase<EventMetadata, string>
{
    public static EventMetadata Create(string fqn)
    {
        return new EventMetadata(fqn);
    }

    public EventMetadata(string fqn) : base(fqn)
    {
    }

    public string Fqn => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<EventMetadata> Rehydrate()
    {
        return (property, _) => new EventMetadata(property);
    }
}