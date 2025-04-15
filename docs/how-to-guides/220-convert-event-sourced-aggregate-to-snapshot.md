# Converting an event-sourced aggregate to a snapshot aggregate

## Why?

You've decided that an event-sourced aggregate is better modelled as a snapshot aggregate.

Common reasons for this:
1. You have an aggregate that is not very large, or it has very too few events in its lifetime. Basically, very little behaviour to model.
2. You have an aggregate has too many events in its lifetime, and it creates more than a couple of thousand events in its lifetime. It will become too slow/expensive to load the events from the event store.

Common examples:
1. A sensor data aggregate that records telemetry events, and appends an event for every change in the sensor. Could create thousands or millions of events ina short time frame.
2. A user aggregate that changes state every time a user uses the software. That could create thousands of events in a year or so.


## What is the mechanism?

The mechanism to make this change is to change the aggregate from event-sourced to snapshot persistence, and convert the existing read model to a read/write model in a database.

You may or may not require changes to data store schemas, and existing data produced by existing read models.

## Where to start?

Consider your existing read models that are produced by projections from your current \[event-sourced\] aggregate.

Constraints:
* You will only be allowed to keep one of the read models that your aggregate currently produces, moving forward. This will become your read/write model, as is the norm for snapshot aggregates. (but is not how event-sourced aggregates work).
* You will need to upgrade your chosen 'primary' read model to include any missing data fields/columns that are needed by your current aggregate to represent its current state in memory. This may or may not be a trivial task, depending on the complexity of your aggregate and the number of events that it has produced in its lifetime.

> You must consider the impact of any changes to the read model for existing users of your software, to ensure an easy transition over when the new version of this software is released to your customers.

Secondly, you will be changing both the aggregate root class, the repository that is used to persist the aggregate, and the primary read model classes.

### Upgrading the aggregate root class

To upgrade your aggregate root, you need to perform the following changes to it:

1. Add an `[EntityName("MyAggregate")]` attribute to the class, using the same name of the chosen read model you already have.

2. Delete the following constructor of the class:
   ```c#
       private MyAggregateRoot(IRecorder recorder, IIdentifierFactory idFactory, ISingleValueObject<string> identifier) : base( recorder, idFactory, identifier)
       {
       }
   ```

3. Add the following constructor to the class:

   ```c#
   using Domain.Common.Extensions;    
   
   	private MyAggregateRoot(ISingleValueObject<string> identifier, IDependencyContainer container, HydrationProperties rehydratingProperties) : base(identifier, container, rehydratingProperties)
       {
           propertyname = rehydratingProperties.GetValueOrDefault<Name>(nameof(propertyname));
           OrganizationId = rehydratingProperties.GetValueOrDefault<Identifier>(nameof(OrganizationId));
       }
   ```

   You MUST also update the property mappings in this method to map all properties of the aggregate that represent its state in memory from properties persisted in the repository.

4. Add the following method to the class:

   ```c#
       public override HydrationProperties Dehydrate()
       {
           var properties = base.Dehydrate();
           properties.Add(nameof(APropertyName), APropertyName);
           properties.Add(nameof(OrganizationId), OrganizationId);
           return properties;
       }
   ```

   You MUST also update the property mappings in this method to map all properties of the aggregate from its state in memory to values to be persisted in the repository.

   > This is where the read and write models become the same thing.

5. Delete the following method from the class:

   ```c#
    [UsedImplicitly]
    public static AggregateRootFactory<MyAggregateRoot> Rehydrate()
    {
        return (identifier, container, properties) => new AuthTokensRootRoot(container.GetRequiredService<IRecorder>(), container.GetRequiredService<IIdentifierFactory>(), identifier);
    }
   ```

6. Add the following method to the class:
   ```c#
    [UsedImplicitly]
    public static AggregateRootFactory<MyAggregateRoot> Rehydrate()
    {
        return (identifier, container, properties) => new AuthTokensRootRoot(identifier, container, properties);
    }
   ```

> You may need to adapt other code in this class (and in unit tests) to use these new signatures.


### Upgrading the repository class

The code changes for the aggregate root, will start using both the `Dehydrate()` and `Rehydrate()` methods to save and load your aggregate from the repository.

> In special cases, where you have sub-entities in your aggregate, things get a little more complicated because you will need to load the state of your aggregate and then load the state of your entities separately. It is likely that you will keep the state of the entities in separate containers (database tables), and thus you need to load their state independently, and in addition to, the state of the aggregate from its container. 
>
> For that purpose, we recommend defining different overloads of a method like `Task<Result<Error>> AggregateAsync(MyEntity entity)` on your root aggregate class, that each append an instance of a sub-entity to the aggregate. Such that, dehydrating a whole aggregate will require one or more calls to the `IDataStore`, and one or more calls to methods like `AggregateAsync()` to populate the full state of the aggregate in memory.

Your current repository class is loading and saving aggregates using a `IEventSourcingDddCommandStore<MyAggregateRoot>`.

We need to switch from using `IEventSourcingDddCommandStore<TAggregateRoot>` to using `ISnapshottingDddCommandStore<TAggregateRoot>`. then we need to use slightly different ways of saving and loading the aggregates state.

1. Change the constructor of your repository to use `ISnapshottingDddCommandStore<TAggregateRoot>`

2. Edit the code of your `LoadAsync()` and instead of calling `IEventSourcingDddCommandStore<TAggregateRoot>.LoadAsync()` you call `ISnapshottingDddCommandStore<TAggregateRoot>.GetAsync()`

3. Edit the code of your `SaveAsync()` and instead of calling `IEventSourcingDddCommandStore<TAggregateRoot>.SaveAsync()` you call `ISnapshottingDddCommandStore<TAggregateRoot>.UpsertAsync()`

4. Now consider the read model you have chosen. Be sure that the read model class (derived from `ReadModelEntity`) that you choose to use for storing your aggregate data (with same name as that of the `[EntityName]` attribute on your aggregate class) has all the same fields and properties in it that you have defined in the `Dehydrate()` and `Rehydrate()` methods of your aggregate root.

   > If your read model entity class is lacking some fields, and you need to add some, you have to figure out how to deal with that at runtime for existing records that already exist in that read model in your current `IDataStore`. 

5. In the module file of your subdomain, it is likely you ned to change how your repository is registered in dependency injection. Before, you would have been registering it like this:

   ```c#
                   services.AddPerHttpRequest<IMyAggregateRepository, MyAggregateRepository>();
                   services.RegisterEventing<MyAggregateRoot, MyAggregateProjection>();
   ```

6. Now, you can simply register it like this:

   ```c#
                   services.AddPerHttpRequest<IMyAggregateRepository, MyAggregateRepository>();
                   services.RegisterEventing<MyAggregateRoot>();
   ```

7. Lastly, you can delete any other read-model classes that should now be unreferenced by any projections, and you can delete the now unused `MyAggregateProjection` class as well.

### Upgrading the read model

You need to upgrade the primary read model of your aggregate. This is the read model you will use in all queries for all aggregates.

Instead of deriving from `ReadModelEntity` your read model class should be derived from `SnapshottedReadModelEntity`.

This adds two new fields to the read model:

```c#
    Optional<DateTime> CreatedAtUtc { get; set;}
    Optional<DateTime> LastModifiedAtUtc { get; set; }
```

### Upgrading deployments

Finally, you may have to change the schema definition of your tables/containers in your `IDataStore`, for example, if you use a relational database, like Microsoft SQLServer or Postgres, you need to change the schema of the table containing your new aggregate.

If this is the case, and you have schema for that `IDataStore` then update the files for that data store.

Aggregates that change from event-sourcing to snapshotting require 2 new columns of data, as part of their definition:

```sql
  [CreatedAtUtc]          [datetime]       NULL,
  [LastModifiedAtUtc]     [datetime]       NULL,
```

> Lastly, if you are using Microsoft SQL Server, you will need to move the seed definition of the old read model from one file (e.g., from: `AzureSQLServer-Seed-Eventing-Generic.sql`) to the other file (e.g., to: `AzureSQLServer-Seed-Snapshotting-Generic.sql`). 
