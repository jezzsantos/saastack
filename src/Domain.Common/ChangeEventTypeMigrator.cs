using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Interfaces.Entities;

namespace Domain.Common;

/// <summary>
///     Provides a <see cref="IEventSourcedChangeEventMigrator" /> that resolves the unknown event with a collection of
///     <see cref="Mappings" />
/// </summary>
public class ChangeEventTypeMigrator : IEventSourcedChangeEventMigrator
{
    private readonly Dictionary<string, string> _typeNameMappings;

    public ChangeEventTypeMigrator() : this(new Dictionary<string, string>())
    {
    }

    public ChangeEventTypeMigrator(Dictionary<Type, Type> typeNameMappings)
    {
        _typeNameMappings = typeNameMappings.ToDictionary(pair => pair.Key.AssemblyQualifiedName!,
            pair => pair.Value.AssemblyQualifiedName!);
    }

    public ChangeEventTypeMigrator(Dictionary<string, string> typeNameMappings)
    {
        _typeNameMappings = typeNameMappings;
    }

    public IReadOnlyDictionary<string, string> Mappings => _typeNameMappings;

    public Result<IDomainEvent, Error> Rehydrate(string eventId, string eventJson, string originalEventTypeName)
    {
        var migratedTypeName = originalEventTypeName;
        if (_typeNameMappings.ContainsKey(migratedTypeName))
        {
            migratedTypeName = _typeNameMappings[originalEventTypeName];
        }

        var eventType = Type.GetType(migratedTypeName);
        if (eventType.NotExists())
        {
            return Error.RuleViolation(
                Resources.ChangeEventMigrator_UnknownType.Format(eventId, originalEventTypeName));
        }

        return new Result<IDomainEvent, Error>(eventJson.FromEventJson(eventType));
    }

    public Result<IDomainEvent, Error> Rehydrate(string eventId, IDomainEvent domainEvent)
    {
        var json = domainEvent.ToEventJson();
        var typeName = domainEvent.GetType().AssemblyQualifiedName!;
        return Rehydrate(eventId, json, typeName);
    }
}