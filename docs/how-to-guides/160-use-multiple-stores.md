# Using Multiple Stores

## Why?

You find yourself needing to do one, of a few common things depending on your situation:

* You find yourself wanting to split your `IDataStore` into separate physical databases, probably to partition your data, or scale up one data set.
* You want to access (read) data from an existing database, that is perhaps from a legacy system.
* You want to store binary data in different technologies. For example, you want to use a data lake for some kinds of binary data, and use blob storage for others.

> Note: we don't yet fully support writing data to existing database tables that are not compatible with `IDehydratableEntity`.
>
> That is, all database tables written to be `IDataStore` technologies are expected to support writing to the columns: `Id (string | NULL)`, `IsDeleted (boolean | null)` and `LastPersistedAtUtc (datetime | NULL)`.

In any case, the default registered implementations for `IDataStore`, `IQueueStore`, `IBlobStore`, `IEventStore` and `IMessageBusStore` that exist in the dependency injection container (defined in `HostExtensions.cs`), are not suitable for a specific `ICustomRepository` that you want to use in one of your subdomains, and you need to define separate connections to those specific repositories (which could also be different technologies than the defaults).

## What is the mechanism?

If you explore the `HostExtensions.cs` file you will see in the `RegisterPersistence()` method, that registers to the DI, instances of  `IDataStore`, `IQueueStore`, `IBlobStore`, `IEventStore` and `IMessageBusStore` .

> Of course, the actual technology adapters that we use in local development, must differ from the ones we want to use in production builds.

> We prefer not to have to run docker containers or other server components on our local machine just so that we can run the code and debug, or test the code. This is because things run slower, and some server components cant be containerized. 
>
> In the case of all the standard stores, we use the `LocalMachineJsonFileStore` to provide a single instance that provides all those kinds of stores on our local machine. The `LocalMachineJsonFileStore` MUST NOT to be deployed and used in production builds!

Updating the registration of these "central" registrations in the DI container, is essentially going to change all the instances of any code where these interfaces are injected, and they will all be using the same technology.

## Where to start?

If you want to only change a specific use of the `IDataStore` (or other store) in your code, you would not change the central registrations. Instead, you would define another instance of the `IDatStore` and manually inject it into the consuming classes where you want to use it.

For example, let's say that you have a custom repository class in your subdomain, that you want to connect to a different Azure SQL server database. Let's assume that we have two different databases (perhaps in different Azure resource groups, or subscriptions), called `database1` and `database2`. Where `database1` is already registered as the "central" instance of `IDataStore` (in `HostExtensions.cs`).

But `database2` is not registered in the DI container, since registering that would overwrite the `IDataStore` to use `database1`!

Let's also assume you want to define a custom repository that you want to use in your specific application layer, and you define it as `ICustomRepository` (derived from `IApplicationRepository` as is usual).

When you implement this custom repository (`ICustomRepository`), you will need to inject into its constructor an instance of `IDataStore` and then hand that off to one of the store implementations (e.g., `SnapshottingStore<TDto>`) as usual.

For example, a typical repository class looks like this,

```c#
public class CarRepository : ICarRepository
{
    private readonly ISnapshottingQueryStore<Car> _carQueries;
    private readonly IEventSourcingDddCommandStore<CarRoot> _cars;
    private readonly ISnapshottingQueryStore<Unavailability> _unavailabilitiesQueries;

    public CarRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<CarRoot> carsStore, IDataStore store)
    {
        _carQueries = new SnapshottingQueryStore<Car>(recorder, domainFactory, store);
        _cars = carsStore;
        _unavailabilitiesQueries = new SnapshottingQueryStore<Unavailability>(recorder, domainFactory, store);
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _carQueries.DestroyAllAsync(cancellationToken),
            _cars.DestroyAllAsync(cancellationToken),
            _unavailabilitiesQueries.DestroyAllAsync(cancellationToken));
    }
#endif

    ... other methods
}
```

> Notice that the `IDataStore` instance is used by all three stores, which means they all use the same technology and the same connection details to the same database.

Now, let's look at how this works at runtime.

You would be registering this custom repository in your subdomain module . For example, in `CarsModule.cs` we see:

```c#
    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                ... other registrations
                services.AddPerHttpRequest<ICarRepository, CarRepository>();
            };
        }
    }
```

Notice that, since we used the registration syntax `services.AddPerHttpRequest<TService, TImplementation>()` our repository class constructor will be automatically injected with the "centrally" registered instance of `IDataStore` already in the DI container, which in our example, would be `database1`.

But this is not what we want. We want a different instance of `IDataStore` that points to `database2`.

So, the first thing to do is create a new instance of the `IDataStore` that connects to `database2`, and then inject it manually into the instance of the actual custom repository class, like this:

```c#
    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                ... other registrations
                services.AddPerHttpRequest<ICarRepository>(c => new CarRepository(
                    c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcingDddCommandStore<CarRoot>>(),
                    AzureSqlServerStore.Create(c.GetRequiredService<IRecorder>(),
                        AzureSqlServerStoreOptions.AlternativeCredentials(
                            c.GetRequiredService<IConfigurationSettings>(), "Database2"))));
            };
        }
    }
```

Notice that we change the registration method, so that we can take control of the dependency injection, so we can be sure we inject the correct instance of the `IDataStore`, to the database we want.

Now, we don't want to inject the SQL database adapter when we are running or testing locally, so we need an alternative version of this registration in `TESTINGONLY`, so we add this:

```c#
    public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                ... other registrations
#if TESTINGONLY
                services.AddPerHttpRequest<ICarRepository>(c => new CarRepository(
                    c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcingDddCommandStore<CarRoot>>(),
                    LocalMachineJsonFileStore.Create(c.GetRequiredService<IConfigurationSettings>(),
                        "Database2:LocalMachineJsonFileStore")));
#else
                services.AddPerHttpRequest<ICarRepository>(c => new CarRepository(
                    c.GetRequiredService<IRecorder>(),
                    c.GetRequiredService<IDomainFactory>(),
                    c.GetRequiredService<IEventSourcingDddCommandStore<CarRoot>>(),
                    AzureSqlServerStore.Create(c.GetRequiredService<IRecorder>(),
                        AzureSqlServerStoreOptions.AlternativeCredentials(
                            c.GetRequiredService<IConfigurationSettings>(), "Database2:SqlServer"))));
#endif
            };
        }
    }
```

Now, we need the correct configuration defined in `appsettings.json ` to support both of these versions of the `IDataStore` adapter:

In `ApiHost1`, and in the `appsettings.json` file we add:

```json
  "ApplicationServices": {
    "Persistence": {
      "LocalMachineJsonFileStore": {
        "RootPath": "./saastack/local"
      },
      "Kurrent": {
        "ConnectionString": "esdb://localhost:2113?tls=false"
      },
      "Database2":{
        "LocalMachineJsonFileStore": {
          "RootPath": "./saastack/local/database2"
        }
      }
    },
```

and in `appsettings.Azure.json`, we would add this:

```json
  "ApplicationServices": {
    "Persistence": {
      "AzureStorageAccount": {
        "AccountName": "",
        "AccountKey": "",
        "ManagedIdentityClientId": ""
      },
      "AzureServiceBus": {
        "ConnectionString": "",
        "NamespaceName": "",
        "ManagedIdentityClientId": ""
      },
      "SqlServer": {
        "DbServerName": "(local)",
        "DbCredentials": "",
        "DbName": "SaaStack",
        "ManagedIdentityClientId": ""
      },
      "Database2": {
        "SqlServer": {
          "DbServerName": "(local)",
          "DbCredentials": "",
          "DbName": "database2",
          "ManagedIdentityClientId": ""
        },
      }
    },
```

At this point, your instance of the `CustomerRepository` class will now have a connection to the database `database2`, while all other code in your host will be using the default `database1`. 

### Custom Mappings

In some cases, you will want to be reading data from containers/tables of a pre-existing `IDataStore` technologies, in scenarios where you are working with legacy data stores.

In these cases, it is likely that the containers/tables will probably not come with the expected schema in the standard form, that the containers/tables that are produced by this codebase, by its convention.

> Relational database technologies will refer to its containers as "tables", its records as "rows" and its values are known as "columns". Whereas in No-SQL technologies, containers could be "documents" or "partitions", records can be "KV pairs", and values could be known as "fields" or "keys" etc. 

The standard form of a container/table assumed by this codebase, for use by `IDataStore`, is a container/table with the fields/keys defined by the `IDehydratableEntity`, which are:

* `Id` (string)
* `IsDeleted` (nullable bool)
* `LastPersistedAtUtc` (nullable datetime)

Further, the `LastPersistedAtUtc` is assumed to be used as the default sorting field, if no other is defined in any specific query.



Thus, it is possible that when reading data from a legacy/pre-existing data container/table, that these specific data values will not be found, and the code that depends on them downstream will fail after loading the data at runtime.

Also, trying to order query results by a column/key that does not exist in the container/table may also fail at runtime (depending on the actual technology used).



What we need, are ways to populate these necessary fields/keys from containers/tables that do not define these fields/keys, and we need to have a way to specify a default ordering field for queries (when none is specified for the query).

#### Overrides

For these specific reasons, we support two kinds of overrides for all data container definitions.

Typically, in any subdomain, you might be reading data in either a `IReadModelEntity` or in some other POCO class, we will call a "DTO".

In either of these cases, you can provide one or both of these `static` methods in that class.

They have specific parameters and method names, and are loaded dynamically at runtime to help the storage layer correctly map legacy data to missing required data of the code.

##### Mapping Field Values

For example, lets say you have a pre-existing, legacy relational database, with its pre-existing table definition, and we would model it in the code like this:

```  
[EntityName("Custom")]
public class CustomReadModel : ReadModelEntity
{
	public Optional<string> CustomID { get; set; }

	public Optional<DateTime> CreatedAt { get; set; }
}
```

Now, at runtime, the column values `Id`, `IsDeleted` and `LastPersistedAtUtc` would not be automatically populated when we read this data, since the legacy container/table probably does not have such columns/keys. 

But we WILL need at least the `Id` value to have a value, after the record/row of this container/table has been being read.


To do this, each entity can define a custom `static` method directly in the class to provide such critical data.

Add to your class a method with this signature, called `FieldReadMappings`:

```c#
    public static IReadOnlyDictionary<string, Func<IReadOnlyDictionary<string, object?>, object?>> FieldReadMappings()
    {
        return new Dictionary<string, Func<IReadOnlyDictionary<string, object?>, object?>>
        {
            {
                nameof(IDehydratableEntity.Id), entity => entity.GetValueOrDefault("CustomID", string.Empty)
            }
        };
    }
```

This method needs to return a dictionary of field/key to function mappings.

This mapping function will, at runtime, be given a dictionary of all values from the record/row, and the mappings defined by this function will be executed in sequence, and then assigned back to the record/row, for use downstream.

Each mapping function returned by this function, will be passed a record/row of data at runtime, containing the data from the legacy container/table record/row, and is expected to return the new value for the specified column/field/key, using the legacy data.

> Both static values, and dynamic values are supported. So, you can either perform a simple mapping from one field/key to another, or you can calculate a resultant values with a function, in the cases that you either have composite key values, or you have to do some type conversion. For example, converting UNIX time stamps in the legacy data, to dotnet DateTime structures in your read model.  

##### Defining Default Query Ordering

When running queries using `IDataStore.Query()` method against a legacy container/table, the default field/key to order the query results is always assumed to be `LastPersistedAtUtc`.

A new value can be defined in any specific query, for example:

```c#
var query = Query.From<CustomReadModel>()
    .WhereAll()
    .OrderBy(rm => rm.CreatedAt)
```



However, if no `OrderBy` is defined in the specific query, then `LastPersistedAtUtc` will always be assumed. 

For legacy container/tables this column name probably does not exist, and executing default ordering on `LastPersistedAtUtc` may cause a runtime error.

To avoid this, define an override that specifies the default ordering for your legacy container/table.

Add to your class a method with this signature, called `DefaultOrderingField`:

```c#
    public static string DefaultOrderingField()
    {
        return "CreatedAt";
    }
```

This method simply returns the static name of the field/Key to order the query results of the legacy container/table.