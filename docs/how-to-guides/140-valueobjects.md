# ValueObjects

## Why?

You want to introduce a piece of data (coming in from the outside world) or introduce a group of related data (from the outside world) in a domain model. The implication is that this data has some predictable and known behavior over time.

This group of related data may have a simple or complicated lifecycle (i.e., it may change values over time and in a constrained way) and thus form a small "[state machine](https://en.wikipedia.org/wiki/Automata_theory)".

The data coming in from the outside world is required to be "valid" in some way (either data type, bounds, ranges, etc.)

An instance of the data does not need to be uniquely identifiable and can be different "by value" (of its component parts).

> In DDD, value objects are very common, far more common than entities. Vale objects are the starting point for most data concepts.
>
> If you are coming from a data modeling background, you are likely to be modeling your domain using data modeling techniques that assume relational databases. In data modeling, concepts in the real world are typically modeled as a normalized table with a primary key (unique identifier). This design pattern is convenient in relational databases, but it is not the general approach in domain modeling. Most concepts in the real world have more complex relationships and less uniqueness, and the behaviors between related concepts should be described and managed more rigorously in domain modeling. Real-world concepts can and should be captured in value objects first before being promoted to entities (with uniqueness, no matter the value/state that they have).

## What is the mechanism?

A value object in DDD is how any data is modeled.

Data, usually in the form of primitives (i.e., `string`, `int`, and `dateTime`), usually has implied meaning and behavior within the context of any subdomain. Value objects are the mechanisms to capture and represent that behavior over the lifecycle of an aggregate.

Value objects are unique only by the internal state they have and are always equal when they share the same internal state. They have no unique identifier.

A value object is a single class (derived from `ValueObjectBase<TValueObject>`) that is composed of zero or more other value objects (or primitives) that together represent its state in memory. It changes its state (in memory) by returning a new immutable instance with updated values. Its state will persist in a repository, and its state is loaded from a repository, along with its parent aggregate or entity.

> There are very specific rules and constraints that govern the design of aggregates, entities, and value objects and their behavior. See [Domain Driven Design](../design-principles/0050-domain-driven-design.md) notes for more details.

## Where to start?

You start with a value object in the Domain Layer project. This project should have already been created and set up for this, see [Domain Layer](040-domain-layer.md) for details on how to do that.

The thing to remember about value objects is that when they change, that constitutes "a change" in the parents/ancestors aggregate's state.

> They cannot change independently of each other. This is the transactional boundary we talk about in DDD.

Unlike aggregates and entities, value objects do not handle domain events. Value objects are affected directly by the raising of domain events, as they collectively represent the change in state of the aggregate root.

### Create the value object

Depending on the data you wish to represent in a value object, you have two choices.

1. If you have a single piece of data say, represented as a primitive value like a `string` or `int` or `DateTime` or even a `List<string>`, you can wrap that value in a `SingleValueObject<TValueObject, TValue>`
2. If you have more than one piece of data or a group of related values, you can wrap them together into a `ValueObjectBase<TValueObject>` with as many values as you need.

> A `SingleValueObjectBase<TValueObject, TValue>` is a special case of the more generalized `ValueObjectBase<TValueObject>`.



To create a value object, start in your subdomain `Domain` project and add a new class.

In the class, type either: `valueobjectsingle` or `valueobjectmultiple`, and complete the live-template.

The next thing to do is work through the `Create()` method of the class, and perform any validation on the incoming values.

> Remember that a value object cannot hold an invalid state at any time.

For example, this is what it would look like for the `Manufacturer` value object in the `CarsDomain`.

```c# 
public sealed class Manufacturer : ValueObjectBase<Manufacturer>
{
    public static readonly IReadOnlyList<string> AllowedMakes = new List<string> { "Honda", "Toyota" };
    public static readonly IReadOnlyList<string> AllowedModels = new List<string> { "Civic", "Surf" };

    public static Result<Manufacturer, Error> Create(int year, string make, string model)
    {
        var newYear = Year.Create(year);
        if (newYear.IsFailure)
        {
            return newYear.Error;
        }

        var newMake = Name.Create(make);
        if (newMake.IsFailure)
        {
            return newMake.Error;
        }

        var newModel = Name.Create(model);
        if (newModel.IsFailure)
        {
            return newModel.Error;
        }

        return Create(newYear.Value, newMake.Value, newModel.Value);
    }

    public static Result<Manufacturer, Error> Create(Year year, Name make, Name model)
    {
        if (make.IsInvalidParameter(m => AllowedMakes.Contains(m), nameof(make), Resources.Manufacturer_UnknownMake,
                out var error1))
        {
            return error1;
        }

        if (model.IsInvalidParameter(m => AllowedModels.Contains(m), nameof(model), Resources.Manufacturer_UnknownModel,
                out var error2))
        {
            return error2;
        }

        return new Manufacturer(year, make, model);
    }

    private Manufacturer(Year year, Name make, Name model)
    {
        Year = year;
        Make = make;
        Model = model;
    }

    public Name Make { get; }

    public Name Model { get; }

    public Year Year { get; }

    [UsedImplicitly]
    public static ValueObjectFactory<Manufacturer> Rehydrate()
    {
        return (property, container) =>
        {
            var parts = RehydrateToList(property, false);
            return new Manufacturer(Year.Rehydrate()(parts[0]!, container), Name.Rehydrate()(parts[1]!, container),
                Name.Rehydrate()(parts[2]!, container));
        };
    }

    protected override IEnumerable<object?> GetAtomicValues()
    {
        return new object[] { Year, Make, Model };
    }
}
```

Some key things to note here:

1. There are several forms of the `Create()` method, for ease of use in code (and also in testing). They always return a `Result<TValueObject, Error>`
2. The `Create()` method must guarantee that any incoming data is valid at creation time.
3. Value objects are immutable, so the internal properties can only be getters (and not setters)
4. The internal properties are themselves other value objects. They can also be primitives.
5. The `GetAtomicValues()` method must be fed all the internal properties contained in the value object.
6. The `Rehydrate()` method must convert serialized strings to a new instance of the value object.

When you only have one value to represent, you can create a `SingleValueObject<TValueObject, TValue>`.

For example, this is what it would look like for the `NumberPlate` value object in the `CarsDomain`.

```c#
public sealed class NumberPlate : SingleValueObjectBase<NumberPlate, string>
{
    public static Result<NumberPlate, Error> Create(string number)
    {
        if (number.IsNotValuedParameter(number, nameof(number), out var error1))
        {
            return error1;
        }

        if (number.IsInvalidParameter(Validations.Car.NumberPlate, nameof(number),
                Resources.NumberPlate_InvalidNumberPlate, out var error2))
        {
            return error2;
        }

        return new NumberPlate(number);
    }

    private NumberPlate(string number) : base(number)
    {
    }

    public string Registration => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<NumberPlate> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new NumberPlate(parts[0]!);
        };
    }
}
```

Some key things to note here:

1. The `Create()` method has the same purpose as before
2. This class maintains a property called `Value` that you can expose with a better name (in this example, it is called `Registration`)
3. The `Rehydrate()` is the same, and you must implement it correctly based on what you are wrapping.
4. No need for the `GetAtomicValues()` method as that, and only be one value.
5. If you are wrapping a `List<TValue>`, then you would use a slightly different version of the `RehydarteToList()` method inside the `Rehydrate()` method to access each item in the list.

> There are numerous examples of value object implementations all around the codebase, it is unlikely that you have a case that is not already covered by other subdomains, so make sure you learn from existing code before reinventing your own patterns.

### Dealing with nulls

One interesting topic of its own that needs a little explanation, with value objects is nullability. The use of `null`.

Quite often data entering the domain layer is coming from the outside world, from infrastructure layers through the application layer and to the domain. It is pretty common to be receiving nullable reference types (i.e., `string?`) for whatever reason, and these find their ways into value objects.

> This codebase has nullable reference type checking turned on, and it also supports an explicit `Optional<TValue>` type specifically for handling nullability.

For a domain modeling perspective, dealing with a `null` is not really a thing. It is a C# thing.

Having no value, a missing value, or "not a value" is definitely a thing, but a `null` value is open to a lot of interpretation, that is best avoided.

We recommend not modeling domains with `null` but using an `Optional<TValue>` instead. The absence of a value is now very explicit and well-supported across the codebase.

This means that if you want to accept a nullable value in the `Create()` method of your value object, you can, but convert it to an `Optional<TValue>` inside the value object from there on.

A couple of caveats:

1. Make sure to manage the value of an `Optional<TValue>` properly when converting from a serialized value in `Rehydrate()` to an `Optional<TValue>` property in your value object. Use one of these extension methods:`.FromValueOrNone()` or `.FromValueOrNone(Func<TValue> converter)` to make sure that you handle `null` values correctly.
2. In a multi-value value object (i.e. a `ValueObjectBase<TValueObject>`), make sure to manage the value of your `Optional<TValue>` property when passing to the `GetAtomicValues()` method. Use the `ValueOrNull`  or `ToValueOrNull(Func<TValue> converter)` methods to ensure that you convert properly.

### Dealing with immutability

Value objects, by definition, are immutable.

That means that no operation on a value object can change its internal value. In programming this is known as a "pure" method.

To ensure the "immutability" of a value object, all properties can only be getters (and not setters), and any methods (or operations) performed on the value object must return a new instance of the value object (with changed data).

* Defining properties and methods on a value object to interrogate its values is okay.

* Defining methods that mutate/change its state is fine as long as they return a new instance of the value object.

* All other methods are treated as suspicious, and the compiler cannot guarantee that the class remains immutable. Therefore, if you want to implement a method that performs some utility on a value object, and that method does not mutate the value object state, and does not return a new instance of another value object, then you need to mark it up with the `SkipImmutabilityCheck` attribute.

The `SkipImmutabilityCheck` attribute serves two purposes:

1. To help the developer be intentional and explicit about the method being implemented, that it should not be used to mutate the value object.
2. It bypasses the code analysis rules in this codebase that check for immutability.

> It is quite common to provided utility functions on value object to help the aggregate perform its use cases. In fact this is recommended for encapsulation purposes.

For example, consider the   [Domain.Shared.Features.cs](https://github.com/jezzsantos/saastack/blob/main/src/Domain.Shared/Features.cs) value object.

You will notice that this value object has numerous methods on it that are used to interrogate its states, such as `HasAny()`, `HasFeature()`, and `HasNone()`, which are all useful utilities.

You will notice that it also has a method called `Denormalize()` which is useful for mapping the value object to other kinds of objects.

Each of these methods is not mutating the value object state, and thus has to be marked with a `SkipImmutabilityCheck` attribute.

### Dealing with collections

You can easily model collections of data or even collections of value objects in another value object.

Create a new value object from a `SingleValueObject<TValueObject, TList>` using the `valueobjectsingle` template.

Essentially, you are maintaining a single list of something rather than a single value.

For example, the `VehicleManagers` in the `CarsDomain`.

```c# 
public sealed class VehicleManagers : SingleValueObjectBase<VehicleManagers, List<Identifier>>
{
    public static readonly VehicleManagers Empty = new([]);

    public static Result<VehicleManagers, Error> Create(string managerId)
    {
        if (managerId.IsNotValuedParameter(managerId, nameof(managerId), out var error))
        {
            return error;
        }

        return new VehicleManagers([managerId.ToId()]);
    }

    private VehicleManagers(List<Identifier> managerIds) : base(managerIds)
    {
    }

    public IReadOnlyList<Identifier> Ids => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<VehicleManagers> Rehydrate()
    {
        return (property, _) =>
        {
            var items = RehydrateToList(property, true, true);
            return new VehicleManagers(items.Select(item => item.ToId()).ToList());
        };
    }

    public VehicleManagers Append(Identifier id)
    {
        var ids = new List<Identifier>(Value);
        if (!ids.Contains(id))
        {
            ids.Add(id);
        }

        return new VehicleManagers(ids);
    }
}
```

Special consideration needs to taken in the `Rehydrate()` method, to use the `RehydrateToList(property, true, true)` variant of the function, since we would be expecting that the rehydrated value will be a list of values.

### Tests

All value objects should be covered to the maximum coverage in unit tests. Every permutation of every use case.

> In fact, every domain object (every aggregate, entity, and value object) should be fully covered by unit tests, since this code is the most critical code in the codebase.

In the `Domain.UnitTests` project of your subdomain, add a test class with the same name as your value object class, with the suffix `Spec`.

In these tests, it is important to cover the `Create()` methods, as well as any other methods you have defined that returned variants of the value object.

For example,

```c#
[Trait("Category", "Unit")]
public class ManufacturerSpec
{
    [Fact]
    public void WhenCreateAndMakeUnknown_ThenReturnsError()
    {
        var result = Manufacturer.Create(Year.Create(Year.MinYear).Value, Name.Create("unknown").Value,
            Name.Create(Manufacturer.AllowedModels[0]).Value);

        result.Should().BeError(ErrorCode.Validation, Resources.Manufacturer_UnknownMake);
    }

    [Fact]
    public void WhenCreateAndModelUnknown_ThenReturnsError()
    {
        var result = Manufacturer.Create(Year.Create(Year.MinYear).Value,
            Name.Create(Manufacturer.AllowedMakes[0]).Value,
            Name.Create("unknown").Value);

        result.Should().BeError(ErrorCode.Validation, Resources.Manufacturer_UnknownModel);
    }

    [Fact]
    public void WhenCreate_ThenReturnsManufacturer()
    {
        var result = Manufacturer.Create(Year.Create(Year.MinYear).Value,
            Name.Create(Manufacturer.AllowedMakes[0]).Value,
            Name.Create(Manufacturer.AllowedModels[0]).Value).Value;

        result.Year.Number.Should().Be(Year.MinYear);
        result.Make.Text.Should().Be(Manufacturer.AllowedMakes[0]);
        result.Model.Text.Should().Be(Manufacturer.AllowedModels[0]);
    }
}
```