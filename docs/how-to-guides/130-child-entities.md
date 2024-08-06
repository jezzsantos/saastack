# Child Entities

## Why?

You want to represent a relationship from an aggregate to either a single entity or to a collection of entities.

> Remember, an entity (and aggregate root) differ from each other "by ID" and "not by value"
>
> If the concept you are modeling does NOT warrant a unique identifier (as opposed to differing "by value"), it probably is not a good candidate for an entity, and you should model your concept with a value object instead.
>
> Entities (in DDD) are actually rarer than you might first think, and you need to be very intentional about them, given that you can also model an entire state machine in a value object as well! Deeply nesting them is also quite rare, unlike in data modeling.
>
> This is where you need to challenge yourself if you are used to data-modeling your domains, because, in data-modeling you tend to model everything as a new table with a unique ID. However, this is not the default position in domain modeling.

## What is the mechanism?

An entity in DDD only differs from an aggregate root in that the aggregate root is at the root of the relationships and that there is only one of them. Not the case for entities.

Other than that, an entity is a mechanism to model a concept that has a unique ID.

An entity is a single class (derived from `EntityBase`) that is composed of zero or more other entities and value objects, that together represent its state in memory. It changes its state (in memory) by raising domain events, then handling those same events, and updating its child entities and value objects with those events (in memory). Its state will persist in a repository, and its state is loaded from a repository, either from raw domain events (i.e., event-sourced) or from traditional collections.

> There are very specific rules and constraints that govern the design of aggregates, entities, and value objects and their behavior. See [Domain Driven Design](../design-principles/0050-domain-driven-design.md) notes for more details.

## Where to start?

You start with an entity in the Domain Layer project. This project should have already been created and set up for this, see [Domain Layer](040-domain-layer.md) for details on how to do that.

The thing to remember about entities is that when they change, that constitutes "a change" in the aggregate's state.

> They cannot change independently of each other. This is the transactional boundary we talk about in DDD.

Child entities handle domain events the same way that an aggregate handles domain events. The process is precisely the same: `RaiseChangeEvent` -> `OnStateChanged()` -> `EnsureInvariants()`

For some of the domain events that you raise, you will want to hand them down to the child/descendant entities to handle, instead of handling them in the aggregate directly.

To do this, we have a special method on the aggregate called `RaiseEventToChildEntity()`, that you will use in the `OnStateChanged()` method of the aggregate, which will both instantiate the new entity instance and pass the domain event to it for it to process.

To process the domain event, your entity handles the event in the same way in its `OnStateChanged()` method.

### Create the entity

To create an entity, start in your subdomain `Domain` project and add a new class.

In the class, type: `entity`, and complete the live-template.

The next thing to do is work through the comments of the class and remove the sections you will not need.

For example, this is what it would look like for the `Unavailability` entity in the `CarsDomain` (which is an event-sourced aggregate).

```c#
public sealed class Unavailability : EntityBase
{
    public static Result<Unavailability, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        RootEventHandler rootEventHandler)
    {
        return new Unavailability(recorder, idFactory, rootEventHandler);
    }

    private Unavailability(IRecorder recorder, IIdentifierFactory idFactory,
        RootEventHandler rootEventHandler) : base(recorder, idFactory, rootEventHandler)
    {
    }

    protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        switch (@event)
        {
            case UnavailabilitySlotAdded added:
            {
                RootId = added.RootId.ToId();
                OrganizationId = added.OrganizationId.ToId();
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }

    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (!ensureInvariants.IsSuccessful)
        {
            return ensureInvariants.Error;
        }

        //TODO: add your other invariant rules here

        return Result.Ok;
    }

    public Optional<Identifier> OrganizationId { get; private set; } = Optional<Identifier>.None;

    public Optional<Identifier> RootId { get; private set; } = Optional<Identifier>.None;
}
```

> Notice that the parent aggregate is represented as the property `RootId`. You can now rename that to be the name of your root aggregate if you want.
>
> In the example above, you will notice that we are handling the domain event `UnavailabilitySlotAdded,` which is the "initial" event that is used to initialize the child entity passed from the aggregate in the call to `RaiseEventToChildEntity()`. This is the analog in the entity to setting the initial state of the aggregate with the `Created` event.

Lastly, notice that a child entity also has an `EnsureInvariants()` method, which works exactly the same way as it does for the aggregate root.

### Changing State

For some entities, is it necessary to change their state in a use case.

The use case is always implemented in the aggregate root, but the aggregate would be delegating the change in the entity to a method in the entity class, to comply with encapsulation and the design principles of [TellDontAsk](https://martinfowler.com/bliki/TellDontAsk.html) and the [Law of Demeter](https://en.wikipedia.org/wiki/Law_of_Demeter).

#### Raising the event

Changing an entity's state is done similarly to how you change the state of an aggregate. You raise a domain event to do that.

> All domain events are defined on the aggregate.

In the case of raising a domain event to change the state of an entity:

1. From an aggregate root method, locate the instance of the specific entity to update. (usually from a collection of entities already stored on the on the aggregate).
2. Provide a method on the entity class, and have the aggregate call that method.
3. In the entity method, perform the usual validation and rules, and raise the domain event to the entity itself (using the `RaiseChangeEvent()` method).
4. The underlying `EntityBase` class will replay the domain event onto the entity, and then it will call `EnsureInvariants()` on the entity.
5. The entity will handle the domain event in the `OnStateChanged()` method and update its internal state from the state in the domain event.
6. This domain event is then passed up to the parent aggregate to handle (via the `RootEventHandler` defined on the constructor of the entity).
7. The aggregate root handles the domain event in its `OnStateChanged()` method, and finally the aggregate's `EnsureInvariants()` method is called, which often calls the `EnsureVariants() ` method on all the entities in the collection (again).

> This process may seem on first-look to be the opposite of raising an event from the aggregate, where the aggregate receives the event first and then delegates it to the entity. But in actuality, the process is not any different at all. As in both cases, the entity's internal state is updated before the aggregate's state is updated.

For example, for the `BookingRoot` entity in the `carsDomain`, a trip begins in the aggregate:

```c# 
    public Result<Error> StartTrip(Location from)
    {
        if (!CarId.HasValue)
        {
            return Error.RuleViolation(Resources.BookingRoot_ReservationRequiresCar);
        }

        var added = RaiseChangeEvent(BookingsDomain.Events.TripAdded(Id, OrganizationId));
        if (added.IsFailure)
        {
            return added.Error;
        }

        var trip = Trips.Latest()!;
        return trip.Begin(from);
    }
```

Notice that the aggregate delegates the call to the specific `trip` entity, and the trip does this:

```c#  
    public Result<Error> Begin(Location from)
    {
        if (BeganAt.HasValue)
        {
            return Error.RuleViolation(Resources.TripEntity_AlreadyBegan);
        }

        var starts = DateTime.UtcNow;
        return RaiseChangeEvent(Events.TripBegan(RootId.Value, OrganizationId.Value, Id, starts, from));
    }
```

Which raises the event to the entity first and then the aggregate last.

#### Handling the raised event

When any event is raised (using the `RaiseChangeEvent()`), the entity will always play the event back onto itself through the `OnStateChanged()` method.

The first thing to do is to handle the event in the `OnStateChanged()` method.

For example,

```c#
protected override Result<Error> OnStateChanged(IDomainEvent @event)
    {
        switch (@event)
        {
            ... other event handlers
               
            case TripBegan changed:
            {
                var from = Location.Create(changed.BeganFrom);
                if (from.IsFailure)
                {
                    return from.Error;
                }

                BeganAt = changed.BeganAt;
                From = from.Value;
                return Result.Ok;
            }
                
            ... other event handlers

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }
```

The main job here is to convert the data in the domain event back into value objects and then set properties on the entity.

> It is important to note that you only need to set properties on the entity if you need to use them in either the rules of other use cases or for mapping in the application class.
> The other thing worth saying (to avoid over-engineering at this stage) is that even if you decide not to represent the data in a property on the entity now (which is optional), you can always add it later; there is no negative impact. YAGNI, don't add it now if you don't need it now. Then, when you need it, you add it.

#### Invariant rules

The second part of raising an event in the entity is the call to the `EnsureInvariants()` method, performed automatically by the `EntityBase` class immediately after it is handled by the `OnStateChanged()` method.

The purpose of the method is to ensure that, at all times, the entity is in a valid state.

> If you remember, one of the rules of aggregates is that (as a whole) they can NOT be invalid at any point in time. This moment is one of those points in time where that is enforced and verified.

Thus, we say that the rules in this method are the "invariant" rules of the entity since they vary very little (if at all) over time.

These rules, can cascade down to another collection of entities and value objects if needed.

For example, in the `Trip`

```c#
    public override Result<Error> EnsureInvariants()
    {
        var ensureInvariants = base.EnsureInvariants();
        if (ensureInvariants.IsFailure)
        {
            return ensureInvariants.Error;
        }

        if (BeganAt.HasValue && !From.HasValue)
        {
            return Error.RuleViolation(Resources.TripEntity_NoStartingLocation);
        }

        if (EndedAt.HasValue && !BeganAt.HasValue)
        {
            return Error.RuleViolation(Resources.TripEntity_NotBegun);
        }

        if (EndedAt.HasValue && !To.HasValue)
        {
            return Error.RuleViolation(Resources.TripEntity_NoEndingLocation);
        }

        return Result.Ok;
    }
```

Some key notes here:

1. Not every state (after an event is raised) requires an invariant rule to be put in place. Focus on those that must be true at all times, or in specific known states.
2. You may want to cascade the rules in child entities or value object collections.
3. In general, use the `RuleViolation` error with a specific description.
4. These rules (and their contexts) should be unit-tested.

### Dealing with collections

Sometimes, you will need to create a collection of entities for your aggregate.

This is usually pretty straightforward; we recommend creating a standard `IReadOnlyList<TEntity>` to achieve this.

In this way:

* You can add only the methods you will need to manipulate the collection (since it is read-only).
* You can add domain-specific methods that make more sense to this specific collection of entities rather than using the more general methods of say `IList<TEntity>`.
* You can even add your own custom `EnsureInvariants()` method to that collection for ease of use by the parent aggregate.

For example, for the `Unavailabilities` collection in the `CarsDomain`:

```c#
public class Unavailabilities : IReadOnlyList<Unavailability>
{
    private readonly List<Unavailability> _unavailabilities = new();

    public Result<Error> EnsureInvariants()
    {
        _unavailabilities
            .ForEach(una => una.EnsureInvariants());

        if (HasIncompatibleOverlaps())
        {
            return Error.RuleViolation(Resources.Unavailabilities_OverlappingSlot);
        }

        return Result.Ok;
    }

    public int Count => _unavailabilities.Count;

    public IEnumerator<Unavailability> GetEnumerator()
    {
        return _unavailabilities.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public Unavailability this[int index] => _unavailabilities[index];

    public void Add(Unavailability unavailability)
    {
        var match = FindMatching(unavailability);
        if (match.Exists())
        {
            _unavailabilities.Remove(match);
        }

        _unavailabilities.Add(unavailability);
    }

    public Unavailability? FindSlot(TimeSlot slot)
    {
        return _unavailabilities.FirstOrDefault(una => una.Slot.Exists() && una.Slot == slot);
    }

    public void Remove(Identifier unavailabilityId)
    {
        var unavailability = _unavailabilities.Find(una => una.Id == unavailabilityId);
        if (unavailability.Exists())
        {
            _unavailabilities.Remove(unavailability);
        }
    }

    private Unavailability? FindMatching(Unavailability unavailability)
    {
        return _unavailabilities
            .FirstOrDefault(u =>
                Overlaps(u, unavailability) && !HasDifferentCause(u, unavailability));
    }

    private bool HasIncompatibleOverlaps()
    {
        return _unavailabilities.Any(current =>
            _unavailabilities.Where(next => IsDifferentFrom(current, next))
                .Any(next => InConflict(current, next)));
    }

    private static bool IsDifferentFrom(Unavailability current, Unavailability next)
    {
        return !next.Equals(current);
    }

    private static bool InConflict(Unavailability current, Unavailability next)
    {
        return Overlaps(current, next) && HasDifferentCause(current, next);
    }

    private static bool Overlaps(Unavailability current, Unavailability next)
    {
        if (current.Slot.NotExists())
        {
            return false;
        }

        if (next.Slot.NotExists())
        {
            return false;
        }

        return next.Slot.IsIntersecting(current.Slot);
    }

    private static bool HasDifferentCause(Unavailability current, Unavailability next)
    {
        return current.IsDifferentCause(next);
    }
}
```

### Tests

All entities should be covered to the maximum coverage in unit tests. Every permutation of every use case.

> In fact, every domain object (every aggregate, entity, and value object) should be fully covered by unit tests, since this code is the most critical code in the codebase.

In the `Domain.UnitTests` project of your subdomain, add a test class with the same name as your entity class, with the suffix `Spec`.

In these tests, it is important to cover the `Create()` methods, and the initializing events, as well as additional use case methods.

For example,

```c#
[Trait("Category", "Unit")]
public class UnavailabilitySpec
{
    private readonly DateTime _end;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly DateTime _start;
    private readonly Unavailability _unavailability;

    public UnavailabilitySpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _start = DateTime.UtcNow;
        _end = _start.AddHours(1);
        _unavailability = Unavailability.Create(_recorder.Object, _idFactory.Object, _ => Result.Ok).Value;
    }

    [Fact]
    public void WhenInitialized_ThenInitialized()
    {
        var timeSlot = TimeSlot.Create(_start, _end).Value;
        var causedBy = CausedBy.Create(UnavailabilityCausedBy.Maintenance, Optional<string>.None).Value;
        
        _unavailability.RaiseChangeEvent(Events.UnavailabilitySlotAdded("acarid".ToId(), "anorganizationid".ToId(),
            timeSlot, causedBy));

        _unavailability.Id.Should().Be("anid".ToId());
        _unavailability.OrganizationId.Should().Be("anorganizationid".ToId());
        _unavailability.CarId.Should().Be("acarid".ToId());
        _unavailability.Slot.Should().Be(timeSlot);
        _unavailability.CausedBy.Should().Be(causedBy);
    }
    ... other tests
}
```

Make sure you test any invariants defined in the `EnsureInvariants()` method.

Make sure to test any other use case methods you define on the entity.
