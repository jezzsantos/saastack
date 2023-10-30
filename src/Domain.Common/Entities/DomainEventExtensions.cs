using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;

namespace Domain.Common.Entities;

public static class DomainEventExtensions
{
    /// <summary>
    ///     Converts the JSON to an <see cref="IDomainEvent" /> of the specified type
    /// </summary>
    public static IDomainEvent FromEventJson(this string eventJson, Type domainEventType)
    {
        return (IDomainEvent)eventJson.FromJson(domainEventType)!;
    }

    /// <summary>
    ///     Converts the <see cref="IDomainEvent" /> to JSON
    /// </summary>
    public static string ToEventJson(this IDomainEvent domainEvent)
    {
        return domainEvent.ToJson(false, StringExtensions.JsonCasing.Pascal) ?? "{}";
    }

    /// <summary>
    ///     Converts the <see cref="domainEvent" /> to a <see cref="EventSourcedChangeEvent" />
    /// </summary>
    public static Result<EventSourcedChangeEvent, Error> ToVersioned(this IDomainEvent domainEvent,
        IIdentifierFactory factory, string entityType, int version)
    {
        var typeName = domainEvent.GetType().Name;
        var typeFullName = domainEvent.GetType().AssemblyQualifiedName!;
        return EventSourcedChangeEvent.Create(entity =>
            {
                var identifier = factory.Create(entity);
                return identifier.Match<Result<ISingleValueObject<string>, Error>>(id => id.Value, error => error);
            }, entityType, typeName, ToEventJson(domainEvent),
            EventMetadata.Create(typeFullName), version);
    }
}