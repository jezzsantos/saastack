namespace Domain.Common.ValueObjects;

public sealed class EventMetadata : SingleValueObjectBase<EventMetadata, string>
{
    public static EventMetadata Create(string fqn)
    {
        return new EventMetadata(fqn);
    }

    private EventMetadata(string fqn) : base(fqn)
    {
    }

    public string Fqn => Value;

    public static ValueObjectFactory<EventMetadata> Rehydrate()
    {
        return (property, _) => new EventMetadata(property);
    }
}