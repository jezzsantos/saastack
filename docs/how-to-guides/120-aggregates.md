# Aggregate Root Design

## Why?

You want to build out your Domain Layer, model a use case (or a group of use cases) in your subdomain, and encapsulate all that into a well-defined and well-tested [transactional] boundary called an Aggregate Root.

You want to avoid creating [anemic domain models](https://martinfowler.com/bliki/AnemicDomainModel.html) that result from writing "transactional scripts" which lead to under-engineered and hard-to-maintain code (non-encapsulated and duplicated behavior all over the codebase) over long periods of time, touched by many hands, who may not understand the full picture.

## What is the mechanism?

Typically, (and we recommend) one aggregate per subdomain.

> There are, however, exceptions to the guideline, as can be seen in a couple of the larger Generic subdomains of this template.

An aggregate root is a single class (derived from `AggregateRootBase`) that is dehydratable/re-hydratable to/from persistence and is composed of zero or more entities and value objects, that together represent its state in memory. It changes its state (in memory) by raising domain events, then handling those same events, and updating its child entities and value objects with those events (in memory). Its' state will be persisted in a repository, and its state is loaded from a repository, either from raw domain events (i.e., event-sourced) or from traditional collections.

> There are very specific rules and constraints that govern the design of aggregates, entities, and value objects and their behavior. See [Domain Driven Design](../design-principles/0050-domain-driven-design.md) notes for more details.

## Where to start?

You start with an aggregate root in the Domain Layer project. This project should have already been created and set up for this, see [Domain Layer](040-domain-layer.md) for details on how to do that.

Before we dive into the aggregate root it needs to be clear to you what the responsibilities of the calling Application Layer are with respect to an Aggregate:

1. The application class, drives the aggregate root. It has only knowledge of the use cases represented by the aggregate root (not the details of how it works), and it knows when to use the specific use cases. Essentially, the application class is the "coordinator".
2. The application class is responsible for creating new instances of the aggregate when needed.
3. The application class is responsible for retrieving, and converting any data required by the aggregate (according to the contract of the use case) into value objects that can be consumed by the aggregate.
4. The application class is responsible for providing any domain services the aggregate use case may need.
5. The application class must handle all errors raised by the aggregate root, and respond accordingly.
6. The application class is responsible for the persistence of changes to the aggregate root in each use case. It is the application class that determines whether the use case is stateful or stateless.

### Initial state

The initial state of an aggregate requires a little thinking upfront as to what the bare minimum information an aggregate needs to be in an initialized state. Ideally, less is more here. It is really about what is non-negotiable, given that an aggregate can never be in an invalid state.

If the subdomain is tenanted, you must include at least the `OrganizationId` as part of the initial state. Most often, that is all that is required to be passed in.

All aggregates/entities and value objects are required to be constructed with class factories and not constructors. Thus if an aggregate needs a certain piece of data (or domain service) to derive its initial state, these need to be provided to the `Create()` class factory method.

In the case of an aggregate, not only is it constructed in the class factory method (`Create()`), but it immediately raises a `Created` event to set up its initial state. Unlike regular OOP objects, the initial state of an aggregate class is set by handling the `Created` event in the `OnStateChanged()` method, not by setting properties directly in the constructor.

You create and set the initial state by:

1. Defining the `Create()` method signature. You must provide this method with the necessary data in value objects (or primitives) with any necessary domain services. If the subdomain is tenanted, you must include at least the `OrganizationId`.
2. These value objects/primitives are then passed onto to the `Created` event as value objects and primitives, and then mapped to primitives in the event itself.
3. The initial state is then set back to value objects and entities in a handler in the `OnStateChanged()` method.

#### Rehydration

When an aggregate is rehydrated from its stored state (sourced from data in a repository), the runtime will use the aggregate's `Rehydrate()` method to construct the instance in memory. The runtime will provide the stored state (and any injected domain services). The `Rehydrate()` method will invoke the constructor to instantiate the instance.

If the aggregate is event-sourced, then no persisted state is provided in the `Rehydrate()` method since after rehydration, the stored events will be played through the `OnStateChanged()` method, one by one (in order), to build up the state of the aggregate on the event at a time.

If the aggregate is snapshotted, the persisted data will need to be mapped into value objects manually by the constructor that the `Rehydrate()` method invokes. Then, the specific application repository will have to apply the data to all the child entities.

> See the `BookingRoot` for an example of a snapshotted aggregate. However, this example does not include populating child entities.

#### Handling the raised event

The last step in initialization is to set the actual state change in memory.

This is done in the `OnStateChanged()` method by handling the raised `Created` event (from the `Create()` method, and setting any initial properties of the aggregate.

For example,

```c#
protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
{
    switch (@event)
    {
        case Created created:
        {
            OrganizationId = created.OrganizationId.ToId();
            Status = created.Status;
            return Result.Ok;
        }
            
        ... other event handlers
            
        default:
            return HandleUnKnownStateChangedEvent(@event);
    }
}
```

> For more complex state machines, like the example above, it is quite common to define an enum, and set a `Status` property to the initial state of the aggregate, in this handler (as opposed to setting a default value for it, as you would in OOP). In memory state-changes should all be grouped in this `OnStateChanged()` method to make it easier for the reader to understand the individual sate changes, and their transitions in one place.

### Changing state

When it comes to changing the state of the aggregate, this MUST be done using the ["Tell Don't Ask"](https://martinfowler.com/bliki/TellDontAsk.html) design pattern.

> This design principle is important for encapsulation.

This means, in practice, that a new method on the aggregate is created to manage the entire change (as an entire transaction).

This single method would normally represent a whole self-contained use case; sometimes, it represents a smaller component part of one or more larger use cases.

#### Raising the event

Create a new method on the aggregate, for example: `SetManufacturer()` in the `CarRoot`:

```c#
    public Result<Error> SetManufacturer(Manufacturer manufacturer)
    {
        return RaiseChangeEvent(CarsDomain.Events.ManufacturerChanged(Id, OrganizationId, manufacturer));
    }
```

The above example is the simplest example of a use case, where:

* A simple value object is passed in,
* There are no rules applied to either who is performing this operation or what state the aggregate is in,
* We raise a single event, and forward the same data to it.

In many cases, things are a little more complex than this, in many dimensions.

For example, `AssignRoles()` in the `OrganizationsRoot`:

```c# 
    public Result<Error> AssignRoles(Identifier assignerId, Roles assignerRoles, Identifier userId, Roles rolesToAssign)
    {
        if (!IsOwner(assignerRoles))
        {
            return Error.RoleViolation(Resources.OrganizationRoot_UserNotOrgOwner);
        }

        if (!IsMember(userId))
        {
            return Error.RuleViolation(Resources.OrganizationRoot_UserNotMember);
        }

        foreach (var role in rolesToAssign.Items)
        {
            if (!TenantRoles.IsTenantAssignableRole(role))
            {
                return Error.RuleViolation(Resources.OrganizationRoot_RoleNotAssignable.Format(role));
            }

            var assigned = RaiseChangeEvent(OrganizationsDomain.Events.RoleAssigned(Id, assignerId, userId, role));
            if (assigned.IsFailure)
            {
                return assigned.Error;
            }
        }

        return Result.Ok;
    }
```

In this example:

* We have several rules to ensure that the calling-user has the correct permissions to execute the use case.
* We also have rules that ensure the aggregate is in the correct state.
* We also raise more than one domain event for this use case.

In all cases, the method always ends by raising at least an event and NEVER sets the internal state of the aggregate directly.

Also, notice that the methods all return a `Result<Error>`.

> These use case methods can return additional results as well, but the bare minimum is a `Result<Error>`

In more complex cases, these aggregate methods can accept delegates that are invoked within the method under certain conditions.

For example,, `RemoveAvatarAsync()` in the `OrganizationsRoot`:

```c#
    public async Task<Result<Error>> RemoveAvatarAsync(Identifier deleterId, Roles deleterRoles,
        RemoveAvatarAction onRemoveOld)
    {
        if (!IsOwner(deleterRoles))
        {
            return Error.RoleViolation(Resources.OrganizationRoot_UserNotOrgOwner);
        }

        if (!Avatar.HasValue)
        {
            return Error.RuleViolation(Resources.OrganizationRoot_NoAvatar);
        }

        var avatarId = Avatar.Value.ImageId;
        var removed = await onRemoveOld(avatarId);
        if (removed.IsFailure)
        {
            return removed.Error;
        }

        return RaiseChangeEvent(OrganizationsDomain.Events.AvatarRemoved(Id, avatarId));
    }
```

These delegates are designed to de-couple the aggregate from:

* Any types defined in the Application Layer,
* The calling application logic, should the calling application wish to provide/retrieve additional data or invoke other application services commands under these specific circumstances.

Neither of these things not concerns for the domain layer.

> In these cases, the aggregate method is required to be `async` since the application might be accessing application services with async functionality.

#### Handling the raised event

When any event is raised (using the `RaiseChangeEvent()`), the aggregate will always play the event back onto itself through the `OnStateChanged()` method.

Then the aggregate will self-validate the state of the aggregate through the `EnsureInvariants()` method.

> This cycle is performed for every event raised.

The first thing to do is to handle the event in the `OnStateChanged()` method.

For example,

```c# 
    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                OrganizationId = created.OrganizationId.ToId();
                Status = created.Status;
                return Result.Ok;
            }

            case ManufacturerChanged changed:
            {
                var manufacturer = CarsDomain.Manufacturer.Create(changed.Year, changed.Make, changed.Model);
                return manufacturer.Match(manu =>
                {
                    Manufacturer = manu.Value;
                    Recorder.TraceDebug(null, "Car {Id} changed manufacturer to {Year}, {Make}, {Model}", Id,
                        changed.Year, changed.Make, changed.Model);
                    return Result.Ok;
                }, error => error);
            }
                
            ... other event handlers
                
            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }
```

The main job here is to convert the data in the domain event back into value objects and then set properties on the aggregate. We also need to trace out the event (at the `TraceDebug` level).

> It is important to note that you only need to set properties on the aggregate if you need to use them in either the rules of other use cases or for mapping in the application class.
> The other thing worth saying (to avoid over-engineering at this stage) is that even if you decide not to represent the data in a property on the aggregate now (which is optional), you can always add it later; there is no negative impact. YAGNI, don't add it now if you don't need it now. Then, when you need it, you add it.

#### Invariant rules

The second part of raising an event in the aggregate is the call to the `EnsureInvariants()` method.

This method is called immediately after the event has been raised, and after it is handled by the `OnStateChanged()` method.

The purpose of the method is to ensure that, at all times, the aggregate is in a valid state.

> If you remember, one of the rules of aggregates is that they can NOT be invalid at any point in time. This moment is one of those points in time where that is enforced and verified.

Thus, we say that the rules in this method are the "invariant" rules of the aggregate since they vary very little (if at all) over time.

These rules, can cascade down the collection of entities and value objects if needed.

For example, in the `CarsRoot`

```c#
public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        var unavailabilityInvariants = Unavailabilities.EnsureInvariants();
        if (unavailabilityInvariants.IsFailure)
        {
            return unavailabilityInvariants.Error;
        }

        if (Unavailabilities.Count > 0)
        {
            if (!Manufacturer.HasValue)
            {
                return Error.RuleViolation(Resources.CarRoot_NotManufactured);
            }

            if (!Owner.HasValue)
            {
                return Error.RuleViolation(Resources.CarRoot_NotOwned);
            }

            if (!License.HasValue)
            {
                return Error.RuleViolation(Resources.CarRoot_NotRegistered);
            }
        }

        return Result.Ok;
    }
```

Some key notes here:

1. Not every state (after an event is raised) requires an invariant rule to be put in place. Focus on those that must be true at all times, or in specific known states.
2. You may want to cascade the rules in child entities or value object collections, as you can see in the example above, with the `Unavailabilities` entities.
3. In general, use the `RuleViolation` error with a specific description.
4. These rules (and their contexts) should be unit-tested.

#### Child entities

All aggregates can support one or more entities and one or more collections of entities. This is a good way to model child/descendant collections in DDD.

See [Child Entities](130-child-entities.md) for designing and building those.

#### Child value objects

All aggregates can support one or more value objects and value objects can also be collection of other value objects.

See [Value Objects](140-valueobjects.md) for designing and building those.

### Deleting

Aggregates can support logical (soft-delete) or physical (hard) deletion, depending on the persistence mechanisms (event-souring versus snapshotting).

| Persistence mechanism | Soft-delete              | Hard-delete                                         |
|-----------------------|--------------------------|-----------------------------------------------------|
| Event-sourced         | Yes - can be resurrected | Yes & No - tombstoning (but can never be destroyed) |
| Snapshotting          | Yes - can be resurrected | Yes                                                 |

#### Event-sourced

When an aggregate is event-sourced, and [logically] deleted, the aggregate raises a special "Tombstone" event that marks the event stream as deleted. Then, the aggregate is saved, in the usual way (by the application class).

Upon reloading the aggregate later, if it has been previously "tombstoned" then a specific `Error.EntityDeleted` error is returned.

The application layer can choose how to handle the error, but typically it aborts the use case, and returns that error, which eventually get turned into a `HTTP 405 - MethodNotAllowed`.

> It is possible to support "soft-delete" of event-sourced aggregates, ignore that error and [logically] resurrect the event stream. The point is that event stream data can never be destroyed during normal operations.

#### Snapshotted

When an aggregate is snapshotted, and [logically] deleted, the aggregate record can e marked as soft-deleted, or physically deleted, by the application repository.

If it is marked as soft-deleted, then it cannot be retrieved unless the repository is specifically asked to retrieve soft-deleted records.

If it does retrieve a soft-deleted record, then the repository can be asked to resurrect that record.

### Tests

All aggregates should be covered to the maximum coverage in unit tests. Every permutation of every use case.

> In fact, every domain object (every aggregate, entity, and value object) should be fully covered by unit tests, since this code is the most critical code in the codebase.

In the `Domain.UnitTests` project of your subdomain, add a test class with the same name as your aggregate root class, with the suffix `Spec`.

In these tests, it is important to cover the `Create()` methods as well as all use case methods.

In each set of tests for each of these methods, as well as testing the result, make sure you test the aggregate properties, and make sure you test the last event raised by the method to ensure that all code is covered.

For example,

```c# 
[Trait("Category", "Unit")]
public class CarRootSpec
{
    private readonly CarRoot _car;

    public CarRootSpec()
    {
        var recorder = new Mock<IRecorder>();
        var identifierFactory = new Mock<IIdentifierFactory>();
        var entityCount = 0;
        identifierFactory.Setup(f => f.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns((IIdentifiableEntity e) =>
            {
                if (e is Unavailability)
                {
                    return $"anunavailbilityid{++entityCount}".ToId();
                }

                return "anid".ToId();
            });
        _car = CarRoot.Create(recorder.Object, identifierFactory.Object,
            "anorganizationid".ToId()).Value;
    }

    [Fact]
    public void WhenCreate_ThenUnregistered()
    {
        _car.OrganizationId.Should().Be("anorganizationid".ToId());
        _car.Status.Should().Be(CarStatus.Unregistered);
    }

    [Fact]
    public void WhenSetManufacturer_ThenManufactured()
    {
        var manufacturer =
            Manufacturer.Create(Year.MinYear + 1, Manufacturer.AllowedMakes[0], Manufacturer.AllowedModels[0]).Value;

        _car.SetManufacturer(manufacturer);

        _car.Manufacturer.Should().Be(manufacturer);
        _car.Events.Last().Should().BeOfType<ManufacturerChanged>();
    }
 
    ... other tests
}
```
