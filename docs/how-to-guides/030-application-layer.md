# Application Layer

## Why?

You probably need to build a new application layer (for a new subdomain) because you want to build a new API. Or, you might need an application layer to handle some other messaging mechanism, like a queue.

## What is the mechanism?

An application layer (for a subdomain) is implemented as a library project and a unit test project.

In the library project, you would be defining (at least one) application class and any other application services and types that you might need to run the application, for example:

* An application class (that implements the application interface, often driven by an API), e.g., `public class CarsApplication : ICarsApplication`
* A repository definition (e.g., `ICarRespository`) to be used by your application class.
* At least one read model entity, for storing the domain_events raised by your aggregate, and that builds out at least one query-able data source that can be used for query APIs.
* A `Resources.resx` file to keep error messages
* Occasionally, you might need to define an "Application Service" interface that your application class will need to do some of its work on.

## Where to start?

### Projects

In your subdomain solution folder, add a new library project for the application.

For example, `CarsApplication.csproj`

Then, create an associated unit testing project in the `Test` solution folder with the same name but with a `.UnitTests` suffix.

> We recommend that you use the installed project templates for this project, using the `SaaStack Unit Test Project`.

For example, `CarsApplication.UnitTests`

In the application project, create a new class with the same name, and create an interface of the same name.

For example, `ICarsApplication.cs` and in `CarsApplication.cs`, we would have:

```c#  
public class CarsApplication : ICarsApplication
{ 
}
```

### Persistence

Create a subfolder called  `Persistence`, and create a repository interface definition for your application.

This interface must derive from `IApplicationRepository` to fully support integration testing.

For example, `ICarRepository.cs`

```c#
public interface ICarRepository : IApplicationRepository
{
}
```

Now, create a sub-folder of the `Persistence` folder called `ReadModels`, and create a read model class that must derive from `ReadModelEntity`, and be decorated with the `[EntityName]` attribute

For example, in `Car.cs`, we would have:

```c#
[EntityName("Car")]
public class Car : ReadModelEntity
{
    public Optional<string> Id { get;set; }
}
```

Lastly, add a new `Resources.resx` file to the project.

### Tests

In the unit test project, add a reference to the library project, and create a test class for the application class.

For example, in `CarApplicationSpec.cs`, we would have code just like this:

```c#
[Trait("Category", "Unit")]
public class CarsApplicationSpec
{
    private readonly CarsApplication _application;
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly Mock<ICarRepository> _repository;

    public CarsApplicationSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _idFactory.Setup(idf => idf.IsValid(It.IsAny<Identifier>()))
            .Returns(true);
        _repository = new Mock<ICarRepository>();
        _repository.Setup(rep => rep.SaveAsync(It.IsAny<CarRoot>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CarRoot root, CancellationToken _) => root);
        _caller = new Mock<ICallerContext>();
        _caller.Setup(c => c.CallerId).Returns("acallerid");
        _application = new CarsApplication(_recorder.Object, _idFactory.Object,
            _repository.Object);
    }
    
    //use testma to create one or more test methods here
}
```

> Almost all application classes will have at least the following dependencies injected into them, and some will have more.

Now, implement the constructor you've defined in the test, back in your application class, and inject those dependencies into your application class constructor. 

