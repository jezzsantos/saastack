using Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using FluentAssertions;
using Moq;
using UnitTesting.Common.Validation;
using Xunit;

namespace Domain.Common.UnitTests.Entities;

[Trait("Category", "Unit")]
public class EntityBaseSpec
{
    private readonly TestEntity _entity;

    public EntityBaseSpec()
    {
        var recorder = new Mock<IRecorder>();
        var idFactory = new Mock<IIdentifierFactory>();
        idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        var dependencyContainer = new Mock<IDependencyContainer>();
        dependencyContainer.Setup(dc => dc.Resolve<IRecorder>())
            .Returns(recorder.Object);
        dependencyContainer.Setup(dc => dc.Resolve<IIdentifierFactory>())
            .Returns(idFactory.Object);

        _entity = TestEntity.Create(recorder.Object, idFactory.Object, _ => Result.Ok);
    }

    [Fact]
    public void WhenConstructed_ThenIdentifierIsAssigned()
    {
        _entity.Id.Should().Be("anid".ToId());
    }

    [Fact]
    public void WhenConstructed_ThenDatesAssigned()
    {
        var now = DateTime.UtcNow;

        _entity.LastPersistedAtUtc.Should().BeNull();
        _entity.CreatedAtUtc.Should().BeNear(now);
        _entity.LastModifiedAtUtc.Should().Be(_entity.CreatedAtUtc);
    }

    [Fact]
    public void WhenChangeProperty_ThenModified()
    {
        _entity.ChangeProperty("avalue");

        _entity.LastModifiedAtUtc.Should().BeNear(DateTime.UtcNow);
    }

    [Fact]
    public void WhenChangePropertyAndSetAggregateEventHandler_ThenEventHandledByHandler()
    {
        object? handledAggregateEvent = null;
        _entity.SetRootEventHandler(o =>
        {
            handledAggregateEvent = o;
            return Result.Ok;
        });

        _entity.ChangeProperty("avalue");

        handledAggregateEvent.Should().BeEquivalentTo(new TestEntity.ChangeEvent
        {
            APropertyName = "avalue"
        });
        _entity.LastModifiedAtUtc.Should().BeNear(DateTime.UtcNow);
    }
}