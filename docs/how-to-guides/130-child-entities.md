# Child Entities

## Why?

You want to represent a relationship from an aggregate to either a single entity or to a collection of entities.

> Remember, an entity (and aggregate root) differ "by ID" and "not by value"
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
