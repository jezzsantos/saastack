# Application Repository

## Why?

You are building an application class and now want to load or save data for your aggregate root, to execute some kind of command (CQS). Or you want to read data from a read model to execute a query (CQS).

## What is the mechanism?

Repositories are a special variant of an "Application Service". That is a "service" that is consumed by the Application Layer.

> Generally, a "service" is a component that manages its own behavior and its own data.

All "Application Services" implement a port, and this port is defined in the Application Layer, either in the [near] scope of the subdomain that defines and consumes it, or in a [wider] scope where it is defined for use by other subdomains (e.g., in the `Application.Services.Shared` project).

## Where to start?

### Define the port

You start by defining the port for the Repository in the Application project of your subdomain.

For example, in the `CarsApplication` project, add a subfolder called `Persistence`.

Add a new interface using the name of your subdomain, use the singular form, and use the suffix `Repository` in the name.

For example, `ICarRespository` (notice the singular form of `Car`)

```
├── CarsApplication.csproj
└── Persistence
    └── ICarRepository.cs
```

Derive your interface from the `IApplicationRepository` interface

> This derivation is important for testing your application in integration testing, as it is used to delete all the data your repository manages.

Lastly, it is likely that you will either be loading and saving your aggregate, and querying your read models using this repository.

There are very well-defined patterns for doing this, which are reused consistently throughout all the repositories in the codebase.

> There is no need to get creative here and create your own naming schemes and interaction patterns; these are very well-tried and tested patterns. Simply use the same patterns naming patterns, and your teammates will be able to discover them more easily as they make changes to your code. As always, learn from the existing patterns in other subdomains before creating your own.

For example, given a typical subdomain, you might define the following interface:

```c#
public interface ICarRepository : IApplicationRepository
{
    Task<Result<CarRoot, Error>> LoadAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken);

    Task<Result<CarRoot, Error>> SaveAsync(CarRoot car, bool reload, CancellationToken cancellationToken);

    Task<Result<CarRoot, Error>> SaveAsync(CarRoot car, CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<Car>, Error>> SearchAllCarsAsync(Identifier organizationId, SearchOptions searchOptions,
        CancellationToken cancellationToken);
    
    ... other methods
}
```

> Pay close attention to the parameters of each method, and the returned type in each case.

### Define the adapter

The repository adapter is actually not implemented in the Application Layer, it is considered infrastructure, and it is very unlikely to ever be shared between two subdomains.

The adapter (typically) lives in the `Infrastructure` project of your subdomain.

For example, in your `Infrastructure` project, add a subfolder called `Persistence`, and add your new class there, using the same name as the port.

For example, in the `CarsInfrastructure` project,

```
├── CarsInfrastructure.csproj
└── Persistence
    └── CarRepository.cs
```

Derive your class from your interface, and implement the missing methods.

For example,

```c#
public class CarRepository : ICarRepository
{
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<CarRoot, Error>> LoadAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<CarRoot, Error>> SaveAsync(CarRoot car, bool reload, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<CarRoot, Error>> SaveAsync(CarRoot car, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    
    public async Task<Result<IReadOnlyList<Car>, Error>> SearchAllCarsAsync(Identifier organizationId,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    
    ...other methods
}
```

The first thing to tackle here is the constructor.

Add a new constructor, and inject into that constructor the following types:

* `IRecorder` - used for logging
* `IDomainFactory` - used to dehydrate your aggregate root and its child entities and value objects.
* Either an `IEventSourcingDddCommandStore<TAggregateRoot>` or an `ISnapshottingDddCommandStore<TAggregateRoot>` - depending on which persistence scheme you are using
* `IDataStore` - this is a port to underlying persistence technology (i.e., Azure SQL, Amazon RDS, MongoDB, Redis, DynamoDB, etc.)

In the constructor, use those injected parameters to construct local implementations of:

* An `IEventSourcingDddCommandStore<TAggregateRoot>` or `ISnapshottingDddCommandStore<TAggregateRoot>` for loading and saving your aggregate root instances
* An `ISnapshottingQueryStore<TReadModel>` - used for querying your read model (coming later)

For example,

```c#
public class CarRepository : ICarRepository
{
    private readonly ISnapshottingQueryStore<Car> _carQueries;
    private readonly IEventSourcingDddCommandStore<CarRoot> _cars;

    public CarRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<CarRoot> carsStore, IDataStore store)
    {
        _carQueries = new SnapshottingQueryStore<Car>(recorder, domainFactory, store);
        _cars = carsStore;
    }

    ... other methods
```

Now, it's time to implement all the methods.

The `DestroyAllAsync()` method is only ever used in `TESTINGONLY` code (and never in production builds), so we need to surround it with a `#if TESTINGONLY .... #endif` block.

> If you forget this detail, the code will not compile in production builds since the methods you are using will not exist in the codebase.

Then, for its implementation, we need to go through all the stores we created in the constructor and destroy their data. Each one of those stores already knows how to destroy their data, so we just execute those methods in a single statement.

> Use the method `Common.Extensions.Tasks.WhenAllAsync()` to perform this operation.

For example,

```c#
using Common.Extensions;

public class CarRepository : ICarRepository
{
    private readonly ISnapshottingQueryStore<Car> _carQueries;
    private readonly IEventSourcingDddCommandStore<CarRoot> _cars;

    public CarRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<CarRoot> carsStore, IDataStore store)
    {
        _carQueries = new SnapshottingQueryStore<Car>(recorder, domainFactory, store);
        _cars = carsStore;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _carQueries.DestroyAllAsync(cancellationToken),
            _cars.DestroyAllAsync(cancellationToken));
    }
#endif
```

Now, we can move on and implement the remaining methods, for both commands and queries (CQS).

They all have well-defined patterns that you can see in all other repositories, and we are basically just delegating to the underlying stores that you already set up in the constructor.

For example,

```c#
public class CarRepository : ICarRepository
{
    private readonly ISnapshottingQueryStore<Car> _carQueries;
    private readonly IEventSourcingDddCommandStore<CarRoot> _cars;

    public CarRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<CarRoot> carsStore, IDataStore store)
    {
        _carQueries = new SnapshottingQueryStore<Car>(recorder, domainFactory, store);
        _cars = carsStore;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _carQueries.DestroyAllAsync(cancellationToken),
            _cars.DestroyAllAsync(cancellationToken));
    }
#endif

    public async Task<Result<CarRoot, Error>> LoadAsync(Identifier organizationId, Identifier id,
        CancellationToken cancellationToken)
    {
        var car = await _cars.LoadAsync(id, cancellationToken);
        if (car.IsFailure)
        {
            return car.Error;
        }

        return car.Value.OrganizationId != organizationId
            ? Error.EntityNotFound()
            : car;
    }

    public async Task<Result<CarRoot, Error>> SaveAsync(CarRoot car, bool reload, CancellationToken cancellationToken)
    {
        var saved = await _cars.SaveAsync(car, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return reload
            ? await LoadAsync(car.OrganizationId, car.Id, cancellationToken)
            : car;
    }

    public async Task<Result<CarRoot, Error>> SaveAsync(CarRoot car, CancellationToken cancellationToken)
    {
        return await SaveAsync(car, false, cancellationToken);
    }
    
    public async Task<Result<IReadOnlyList<Car>, Error>> SearchAllCarsAsync(Identifier organizationId,
        SearchOptions searchOptions, CancellationToken cancellationToken)
    {
        var queried = await _carQueries.QueryAsync(Query.From<Car>()
            .Where<string>(u => u.OrganizationId, ConditionOperator.EqualTo, organizationId)
            .WithSearchOptions(searchOptions), cancellationToken: cancellationToken);
        if (queried.IsFailure)
        {
            return queried.Error;
        }

        var cars = queried.Value.Results;
        return cars;
    }
    
    ...other methods
}
```

> Notice the two variations of the `SaveAsync()` method used here. They are useful in rare cases where you Save() your aggregate and then have to Save() it again after raising more events later in the consuming code. You need to reload the aggregate between these Saves(); otherwise, you will have an aggregate in a "depleted" state, where it cannot raise any new events.

Last note, if you are using Snapshotting persistence for your aggregate, and you have child entities to persist, you will need to expand your `LoadAsync()` and `SaveAsync()` methods to load and save not only the aggregate to and from the store, but also all of your child entities as well.

> Unfortunately, we don't yet have a good example in the codebase for you to learn from on how to do that effectively for child entities.

### Read model projection

In order to have data in a form that you can query it (i.e., run queries over it), you need to have built a "projection" that creates a "read model". A "projection" is a special kind of read-only repository that you can see being consumed in the `SearchAllCarsAsync` example above.

> Aggregates are not directly query-able if they are persisted using event-sourcing.

See  [Persistence](../design-principles/0070-persistence.md) for more details on how projections work.

> If you are using event-sourcing for your aggregate, you will need to build at least one projection if you want to query any data it produces.

#### Create the projection

The projection (typically) lives in the `Infrastructure` project of your subdomain.

For example, in your `Infrastructure` project, add a subfolder called `Persistence\ReadModels` and add your new class there.

Name your new class after the subdomain, using the suffix `Projection`.

For example, in the `CarsInfrastructure` project,

```
├── CarsInfrastructure.csproj
└── Persistence
    └── ReadModels
        └── CarProjection.cs
```

Derive your class from the `IReadModelProjection` interface, and implement the missing methods.

For example,

```c#
public class CarProjection : IReadModelProjection
{
    public Type RootAggregateType { get; }
    
    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
```

#### Create the read model entity

The next thing you need to do, before you go any further, is to create a "read model" entity class, in the Application Layer of your subdomain.

In your Application project, in the subfolder `Persistence/ReadModels` define a new class by the name of your subdomain.

For example:

```
├── CarsApplication.csproj
└── Persistence
    └── ReadModels
        └── Car.cs
```

Next derive your class from `ReadModelEntity`, and decorate your class with the `[EntityName]` attribute

For example,

````c# 
[EntityName("Car")]
public class Car : ReadModelEntity
{
}
````

Next, add a property to this class that you know you will be reading in a query.

These properties must use the `Optional<T>` for primitives, and can also include any value objects.

For example,

```c#
[EntityName("Car")]
public class Car : ReadModelEntity
{
    public Optional<string> LicenseJurisdiction { get; set; }

    public Optional<string> LicenseNumber { get; set; }

    public VehicleManagers ManagerIds { get; set; } = VehicleManagers.Create();

    public Optional<string> ManufactureMake { get; set; }

    public Optional<string> ManufactureModel { get; set; }

    public Optional<int> ManufactureYear { get; set; }

    public Optional<string> OrganizationId { get; set; }

    public Optional<CarStatus> Status { get; set; }

    public Optional<string> VehicleOwnerId { get; set; }
}
```

> Note: you will add more properties as you start building your projection (below)

#### Finish the projection

Back in your projection class, the next thing to tackle here is the constructor.

Add a new constructor, and inject into that constructor the following types:

* `IRecorder` - used for logging
* `IDomainFactory` - used to dehydrate your aggregate root and its child entities and value objects.
* `IDataStore` - this is a port to underlying persistence technology (i.e., Azure SQL, Amazon RDS, MongoDB, Redis, DynamoDB etc.)

In the constructor, use those injected parameters to construct local implementations of:

* An `IReadModelStore<TReadModel>` for creating, updating, and deleting your read model instances

For example,

```c#
public class CarProjection : IReadModelProjection
{
    private readonly IReadModelStore<Car> _cars;

    public CarProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _cars = new ReadModelStore<Car>(recorder, domainFactory, store);
    }

	public Type RootAggregateType { get; }
    
    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
```

The next thing to do is define the specific `RootAggregateType` for your subdomain.

For example,

```c#
public class CarProjection : IReadModelProjection
{
    private readonly IReadModelStore<Car> _cars;

    public CarProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _cars = new ReadModelStore<Car>(recorder, domainFactory, store);
    }

	public Type RootAggregateType => typeof(CarRoot);
    
    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
```

Lastly, we need to add a `switch` statement and handle all the events that could be raised from your aggregate root.

> Warning: You MUST add handlers (`case` statements) for all the events that your aggregate root could raise.
>
> If you don't handle them in the projection (and at least `return true`), you will receive a runtime exception whenever those events are raised by your aggregate.
>
> This is a necessary precaution (early warning) to remind you not to forget to handle your new event in this class, when you adda new event to your aggregate. You should always handle every event in the projection, even if it does not change any read model.

For example,

```c#
public class CarProjection : IReadModelProjection
{
    private readonly IReadModelStore<Car> _cars;

    public CarProjection(IRecorder recorder, IDomainFactory domainFactory, IDataStore store)
    {
        _cars = new ReadModelStore<Car>(recorder, domainFactory, store);
    }

    public async Task<Result<bool, Error>> ProjectEventAsync(IDomainEvent changeEvent,
        CancellationToken cancellationToken)
    {
        switch (changeEvent)
        {
            case Created e:
                return await _cars.HandleCreateAsync(e.RootId, dto =>
                {
                    dto.OrganizationId = e.OrganizationId;
                    dto.Status = e.Status;
                }, cancellationToken);

            //other events with their own handlers (see the code examples for more details)
            case ManufacturerChanged e:
            case OwnershipChanged e:
            case RegistrationChanged e:
            case UnavailabilitySlotAdded e:
            case UnavailabilitySlotRemoved e:
                return true;

            default:
                return false;
        }
    }

    public Type RootAggregateType => typeof(CarRoot);
}
```

> Note: in the case of the actual `CarsProjection.cs` you will notice that it creates two read models (a.k.a. database tables) from handling all these events (e.g. a `Cars` read model, and a `Unavailabilities` read model).
>
> You will also notice the use of `HandleCreateAsync`, `HandleUpdateAsync`, and `HandleDeleteAsync` methods in the projection.

### Inject your adapters

The last step to finish building out your repositories is to register them in the DI container so that they are injected into your application class as runtime.

In your `Infrastructure` project, edit your `Module.cs` files, and change your `RegisterServices()` method.

Make sure you have registered both the `Repository.cs` class, and the `Projection.cs` class for your subdomain.

For example, in the `CarsModule.cs`:

```c#
public Action<ConfigurationManager, IServiceCollection> RegisterServices
    {
        get
        {
            return (_, services) =>
            {
                services.AddPerHttpRequest<ICarsApplication, CarsApplication.CarsApplication>();
                services.AddPerHttpRequest<ICarRepository, CarRepository>();
                services.RegisterEventing<CarRoot, CarProjection>(
                    c => new CarProjection(c.GetRequiredService<IRecorder>(),
                        c.GetRequiredService<IDomainFactory>(),
                        c.GetRequiredService<IDataStore>())
                );

                services.AddPerHttpRequest<ICarsService, CarsInProcessServiceClient>();
            };
        }
    }
```

See the [dependency injection](../design-principles/0060-dependency-injection.md) for more details 