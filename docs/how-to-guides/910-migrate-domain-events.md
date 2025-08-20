# Migrating Domain Events

## Why?

You are changing one of the domain events that already exist in production, and making a breaking change, that would break production data with your new production code.

You need a way to make either breaking or non-breaking changes to your events with respect to existing data that is already present in your `IEventStore` for the existing event.

If you cannot just add a new event and must replace the existing event.

## What is the Mechanism?

In most event sourced systems, the raw domain events are stored in some repository. e.g., an event store.

This data can be stored in many formats, but by default, it is stored in a JSON format that is easily readable by humans using the tools provided  for diagnostic and debugging purposes.

When the domain event is "dehydrated" and put into an Event Store, the domain event data is first serialized into JSON, and the C# class name (fully qualified) is also saved within the metadata that is saved in the Event Store.

The raw JSON data of any domain event might look something like this:
```json
{
  "Id": "event_1234567890123456789012",
  "EntityName": "AnAggregate",
  "EntityType": "AnAggregateRoot",
  "EventType": "Happened",
  "Data": "{\u0022OccurredUtc\u0022:\u00222025-08-20T02:20:51.4608065Z\u0022,\u0022RootId\u0022:\u0022aroot_123457890123456789012\u0022,\u0022AProperty\u0022: \u0022avalue\u0022}",
  "Metadata": "Domain.Events.Shared.TestingOnly.Happened,  Domain.Events.Shared, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
  "Version": 1,
  "StreamName": "AnAggregate_aroot_123457890123456789012",
  "LastPersistedAtUtc": null,
  "IsDeleted": "False",
  "IsTombstone": false
}
```

> This may not be how the data looks in any specific Event Store technology. The point is that both the `Data` and `Metadata` properties will always be persisted, and the `Data` will be stored either as encoded JSON, or as some other format derived from the encoded JSON (e.g., a binary blob).

Later, at runtime, when an aggregate instance is "hydrated" from this Event Store, the stored `Data` will be deserialized into a new instance of the C# class referenced by the class in the `Metadata` property of the domain event by the JSON Serializer (`System.Text.Json.JsonSerializer`). The deserialized instance of this class is then consumed by the aggregate class, to rebuilt its internal state.

Thus, if you make certain changes about the C# definition of the existing domain event, you might experience failures at runtime, should the JSON serializer fail to deserialize your stored data into your C# class definition, due to your changes.

## Where to start?

Start by being aware that certain changes to domain events are totally safe, and are naturally backwards compatible with former definitions of themselves. Whereas some changes to domain events cannot be backwards compatible, and are thus designated as "breaking changes".

Examples of non-breaking changes:

* Adding a new nullable property, with or without a default value.
* Removing an existing property (nullable or required)

Examples of breaking changes:

* Renaming the class
* Adding a `required` property, or changing a nullable to `required`
* Renaming a property
* Changing the datatype of a property

To manage any breaking changes (can also be used for non-breaking changes), we need to talk about versioning your domain event class declarations, and migrating the data between them.

> This version number is not to be confused with the version number of a specific event that is related to their chronological ordering, with respect to an aggregate instance.

### Create New Event

1. Find the existing domain event class that you want to change. e.g. the `Happened` domain event.
2. Copy and paste the class definition to the same file, below the original definition, and keep the same filename.
3. Rename the copied class, and append a version number to its class name. e.g. `HappenedV2`
   - Start at the number 2, and only use whole numbers
4. Add the `[ObsoleteAttribute]` to the original event class, as documentation. e.g. `[Obsolete($"Use {nameof(HappenedV2) instead}")]`
5. You can optionally inherit your new class from the old class, but it is not recommended, as it limits the changes, or certain changes harder to define for your new class. Cloning and modifying is a better way to proceed.

> This technique is not the only method, but using it recommended, as it keeps a permanent record of the old class definition for future testing and documentation, for those seeing this class for the first time later.

### Set data defaults

If you add new properties to your new domain event, you have to anticipate and accept that these properties may not have values in the existing data of the persisted events in the Event Store, and thus your new event may not have the necessary data stored in past events to populate these values.

If these properties are defined as nullable, then your new code must deal with the possibility of dealing with a `null` value in this new event.

> That means, all aggregates (and all message-bus subscribers) must handle `null` for that property.

If these properties are declared as non-nullable, you will need to provide a default value for the property.

You can do that in a couple of ways in C#. You can either hardcode a default value on the property setter, or dynamic property on the setter, or set the value in the constructor, either statically or dynamically, for example if you rename an existing property.

You CANNOT add a new `required` property to your new event, as the default JSON serializer will not be able to populate this property from the JSON of the old class definition.

For example:

```c#
[Obsolete($"Use {nameof(HappenedV2)} instead")]
public class Happened : DomainEvent
{
    public Happened(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Happened()
    {
    }

    public required string Message1 { get; set; }
}

public sealed class HappenedV2 : DomainEvent
{
    public HappenedV2(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public HappenedV2()
    {
    }

    [JsonIgnore] public string? Message1 { get; set; }

    public string Message2 { get; set; } = "amessage2"; // A new property with a default value

    [JsonPropertyName(nameof(Message1))]
    public string Message3 { get; set; } = string.Empty; // A renamed property
}
```

### Change Aggregate Root

Next, replace your old event class in the code of the aggregate root (and tests), so that your aggregate root now handles the new event, not the old event.

>  You can be guided by the compiler warnings for the use of the now `[Obsolete]` older domain event class

### Change Projections

Next, replace your old event class in the code of any projections you have, so that your projection now handles the new event, not the old event.

>  You can be guided by the compiler warnings for the use of the now `[Obsolete]` older domain event class

### Register automatic migration

Finally, in `HostExtensions.cs`, and in a function called `RegisterPersistence()`, find the registration of the `ChangeEventTypeMigrator`

```c#
            services.AddSingleton<IEventSourcedChangeEventMigrator>(c => new ChangeEventTypeMigrator(
                new Dictionary<string, string>
                {
                    { typeof(Happened), typeof(HappenedV2) }
                }));
```

Add the mapping from the old event class to the new event class in this dictionary.

> Try to avoid using raw strings in this definition, and use `typeof()` as this makes the code easier to understand later. 

#### How the migrator works

The `ChangeEventTypeMigrator` will now deserialize the existing events from the Event Store, that match the event name on the left side (fully qualified) and when those events are encountered, will then instantiate the class on the right with the serialized JSON from the event store.

Thus, any mapping of existing properties to new properties of your class, is done within the class itself, not by the migrator.

### Write migration tests

