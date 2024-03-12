using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using FluentAssertions;
using Moq;
using UnitTesting.Common.Validation;
using Xunit;

namespace Infrastructure.Common.UnitTests;

[Trait("Category", "Unit")]
public class DomainFactorySpec
{
    private readonly DomainFactory _factory;

    public DomainFactorySpec()
    {
        var recorder = new Mock<IRecorder>();
        var identifierFactory = new Mock<IIdentifierFactory>();
        identifierFactory.Setup(f => f.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        var dependencyContainer = new Mock<IDependencyContainer>();
        dependencyContainer.Setup(dc => dc.GetRequiredService<IRecorder>())
            .Returns(recorder.Object);
        dependencyContainer.Setup(dc => dc.GetRequiredService<IIdentifierFactory>())
            .Returns(identifierFactory.Object);

        _factory = new DomainFactory(dependencyContainer.Object);
    }

    [Fact]
    public void WhenRegisterAndNoAssemblies_ThenRegistersNone()
    {
        _factory.RegisterDomainTypesFromAssemblies();

        _factory.EntityFactories.Count.Should().Be(0);
        _factory.ValueObjectFactories.Count.Should().Be(0);
    }

    [Fact]
    public void WhenRegisterAndAssemblyContainsNoFactories_ThenRegistersNone()
    {
        _factory.RegisterDomainTypesFromAssemblies(typeof(Exactly).Assembly);

        _factory.EntityFactories.Count.Should().Be(0);
        _factory.ValueObjectFactories.Count.Should().Be(0);
    }

    [Fact]
    public void WhenRegisterAndAssemblyContainsFactories_ThenRegistersFactories()
    {
        _factory.RegisterDomainTypesFromAssemblies(typeof(DomainFactorySpec).Assembly);

        _factory.EntityFactories.Count.Should().Be(1);
        _factory.EntityFactories.First().Key.Should().Be(typeof(TestPersistableEntity));
        _factory.ValueObjectFactories.Count.Should().Be(1);
        _factory.ValueObjectFactories.First().Key.Should().Be(typeof(TestValueObject));
    }

    [Fact]
    public void WhenRehydrateEntityAndTypeNotExist_ThenThrows()
    {
        _factory
            .Invoking(x => x
                .RehydrateEntity(typeof(TestPersistableEntity),
                    new HydrationProperties())).Should()
            .Throw<InvalidOperationException>()
            .WithMessageLike(Resources.DomainFactory_EntityTypeNotFound);
    }

    [Fact]
    public void WhenRehydrateEntityAndExists_ThenReturnsEntityInstance()
    {
        _factory.RegisterDomainTypesFromAssemblies(typeof(DomainFactorySpec).Assembly);

        var result = (TestPersistableEntity)_factory.RehydrateEntity(
            typeof(TestPersistableEntity), new HydrationProperties
            {
                { nameof(IIdentifiableEntity.Id), "anid".ToId() },
                { nameof(TestPersistableEntity.APropertyValue), "avalue" }
            });

        result.Id.Should().Be("anid".ToId());
        result.APropertyValue.Should().Be("avalue");
    }

    [Fact]
    public void WhenRehydrateEntityForOptionalAndExists_ThenReturnsEntityInstance()
    {
        _factory.RegisterDomainTypesFromAssemblies(typeof(DomainFactorySpec).Assembly);

        var result = (TestPersistableEntity)_factory.RehydrateEntity(
            typeof(Optional<TestPersistableEntity>), new HydrationProperties
            {
                { nameof(IIdentifiableEntity.Id), "anid".ToId() },
                { nameof(TestPersistableEntity.APropertyValue), "avalue" }
            });

        result.Id.Should().Be("anid".ToId());
        result.APropertyValue.Should().Be("avalue");
    }

    [Fact]
    public void WhenRehydrateValueObjectAndTypeNotExist_ThenThrows()
    {
        _factory
            .Invoking(x => x
                .RehydrateValueObject(typeof(TestValueObject), "avalue")).Should()
            .Throw<InvalidOperationException>()
            .WithMessageLike(Resources.DomainFactory_ValueObjectTypeNotFound);
    }

    [Fact]
    public void WhenRehydrateValueObjectAndExists_ThenReturnsEntityInstance()
    {
        _factory.RegisterDomainTypesFromAssemblies(typeof(DomainFactorySpec).Assembly);

        var result = (TestValueObject)_factory.RehydrateValueObject(typeof(TestValueObject), "avalue");

        result.APropertyValue.Should().Be("avalue");
    }

    [Fact]
    public void WhenRehydrateValueObjectForOptionalAndExists_ThenReturnsEntityInstance()
    {
        _factory.RegisterDomainTypesFromAssemblies(typeof(DomainFactorySpec).Assembly);

        var result = (TestValueObject)_factory.RehydrateValueObject(typeof(Optional<TestValueObject>), "avalue");

        result.APropertyValue.Should().Be("avalue");
    }
}