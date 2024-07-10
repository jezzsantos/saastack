# Domain Layer

## Why?

You probably need to build a new domain layer (for a new subdomain) because you want to build a new API.

## What is the mechanism?

A domain layer (for a subdomain) is implemented as a library project and a unit test project.

In the library project, you would be defining (at least one) aggregate root class, a set of domain events, and any other value objects you might need, for example:

* A root aggregate class, e.g., `public class CarRoot: AggregateRootBase`
* An `Events` class that contains one or more factories of events
* A `Validations` class containing one or more validation expressions used to validate data entering the API and domain types
* A `Resources.resx` file to keep error messages

## Where to start?

### Projects

In your subdomain solution folder, add a new library project for the domain.

For example, `CarsDomain.csproj`

Then, create an associated unit testing project in the `Test` solution folder with the same name but with a `.UnitTests` suffix.

> We recommend that you use the installed project templates for this project, using the `SaaStack Unit Test Project`.

For example, `CarsDomain.UnitTests`

### Additional files

Create a new resource file called `Resources.resx`.

Create a new class called `Validations`, and add an example validation expression

For example,

```c# 
public static class Validations
{
    // TODO: delete me
    public static readonly Validation Name = CommonValidations.DescriptiveName();
}
```

### Events

Create a new class called `Events', and add the following code:

```c#
public static class Events
{
    public static Created Created(Identifier id, Identifier organizationId)
    {
        return new Created(id)
        {
            OrganizationId = organizationId,
            Status = CarStatus.Unregistered
        };
    }
}
```

Now, in the `Domain.Events.Shared` project, create a subfolder for the name of your subdomain, and create a new class called `Created`, but use the `Add -> SaaStack -> Domain Event` template to create the class.

Type `OrganizationId` for the name of the added property

Your class should look like this:

```c# 
public sealed class Created : DomainEvent
{
    public Created(Identifier id) : base(id)
    {
    }

    [UsedImplicitly]
    public Created()
    {
    }

    public required string OrganizationId { get; set; }
}
```

Make sure that in your `Events.cs` class in your domain project, you are now referring to the correct definition of the `Created` event, as there are many of them in each of the folders in the `Domain.Events.Shared` project.

### Aggregate root

You need to decide at this point whether you are going to be persisting the state of your aggregate root as event-sourced, or more traditionally using snapshot storage (see the [persistence patterns](../design-principles/0070-persistence.md) for more information).

We recommend using event-sourced persistence for most domains.

In the domain project, create a new class for your root aggregate using an appropriate name for your root.

> We would normally use the suffix `Root` here to make this type stand out as the top-level domain object, but it is optional.

Create the class, and then delete the generated code for the default class definition, and then type `aggregate` and hit ENTER.

Then, type the name of your aggregate (without the suffix `Root`) - the template will add the suffix at this point.

Then, hit TAB to edit the other properties in the template.

Then, follow the instructions in the file and delete the relevant sections.

For example, you should end up with a class defined something like this (if you chose an event-sourced persisted aggregate).

```c#
public sealed class CarRoot : AggregateRootBase
{
    public static Result<CarRoot, Error> Create(IRecorder recorder, IIdentifierFactory idFactory,
        Identifier organizationId)
    {
        var root = new CarRoot(recorder, idFactory);
        root.RaiseCreateEvent(DocumentSigningsDomain.Events.Created(root.Id, organizationId));
        return root;
    }

    private CarRoot(IRecorder recorder, IIdentifierFactory idFactory) : base(recorder, idFactory)
    {
    }

    private CarRoot(IRecorder recorder, IIdentifierFactory idFactory,
        ISingleValueObject<string> identifier) : base(
        recorder, idFactory, identifier)
    {
    }

    public Identifier OrganizationId { get; private set; } = Identifier.Empty();

    public static AggregateRootFactory<CarRoot> Rehydrate()
    {
        return (identifier, container, properties) => new CarRoot(container.GetRequiredService<IRecorder>(),
            container.GetRequiredService<IIdentifierFactory>(), identifier);
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

    protected override Result<Error> OnStateChanged(IDomainEvent @event, bool isReconstituting)
    {
        switch (@event)
        {
            case Created created:
            {
                OrganizationId = created.OrganizationId.ToId();
                return Result.Ok;
            }

            default:
                return HandleUnKnownStateChangedEvent(@event);
        }
    }
}
```

### Tests

In the unit test project, add a reference to the library project and create a test class for the root class.

For example, in `CarRootSpec.cs`, we would have code just like this:

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
            .Returns("anid".ToId());
        _car = CarRoot.Create(recorder.Object, identifierFactory.Object,
            "anorganizationid".ToId()).Value;
    }

    [Fact]
    public void WhenCreate_ThenUnregistered()
    {
        _car.OrganizationId.Should().Be("anorganizationid".ToId());
    }
    
    ... other tests
}
```
