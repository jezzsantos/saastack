using Common;
using Common.Recording;
using Domain.Common.Entities;
using Domain.Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.Services;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace Domain.Common.UnitTests.Entities;

[Trait("Category", "Unit")]
public class AggregateRootBaseSpec
{
    private readonly TestAggregateRoot _aggregate;
    private readonly Mock<IDependencyContainer> _dependencyContainer;
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;
    private readonly ChangeEventTypeMigrator _typeMigrator;

    public AggregateRootBaseSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        _dependencyContainer = new Mock<IDependencyContainer>();
        _dependencyContainer.Setup(dc => dc.Resolve<IRecorder>())
            .Returns(_recorder.Object);
        _dependencyContainer.Setup(dc => dc.Resolve<IIdentifierFactory>())
            .Returns(_idFactory.Object);
        _typeMigrator = new ChangeEventTypeMigrator();

        _aggregate = TestAggregateRoot.Create(_recorder.Object, _idFactory.Object);
    }

    [Fact]
    public void WhenCreated_ThenIdentifierIsAssigned()
    {
        _aggregate.Id.Should().Be("anid".ToId());
    }

    [Fact]
    public void WhenCreated_ThenDatesAssigned()
    {
        var now = DateTime.UtcNow;

        _aggregate.LastPersistedAtUtc.Should().BeNone();
        _aggregate.CreatedAtUtc.Should().BeNear(now);
        _aggregate.LastModifiedAtUtc.Should().BeAfter(_aggregate.CreatedAtUtc);
    }

    [Fact]
    public void WhenCreated_ThenVersioningIsZero()
    {
        _aggregate.EventStream.FirstEventVersion.Should().Be(0);
        _aggregate.EventStream.LastEventVersion.Should().Be(0);
    }

    [Fact]
    public void WhenCreated_ThenRaisesEvent()
    {
        _aggregate.Events.Count.Should().Be(1);
        _aggregate.Events.Last().Should().BeOfType<TestAggregateRoot.CreateEvent>();
        _aggregate.LastModifiedAtUtc.Should().BeAfter(_aggregate.CreatedAtUtc);
    }

    [Fact]
    public void WhenEqualsWithSameTypesButDifferentId_ThenReturnsFalse()
    {
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid1".ToId());
        var aggregate1 = TestAggregateRoot.Create(_recorder.Object, _idFactory.Object);
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid2".ToId());
        var aggregate2 = TestAggregateRoot.Create(_recorder.Object, _idFactory.Object);

        var result = aggregate1.Equals(aggregate2);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithDifferentTypesButSameId_ThenReturnsFalse()
    {
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        var aggregate1 = TestAggregateRoot.Create(_recorder.Object, _idFactory.Object);
        var aggregate2 = TestAggregateRoot2.Create(_recorder.Object, _idFactory.Object);

        // ReSharper disable once SuspiciousTypeConversion.Global
        var result = aggregate1.Equals(aggregate2);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithSameTypeAndSameId_ThenReturnsTrue()
    {
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        var aggregate1 = TestAggregateRoot.Create(_recorder.Object, _idFactory.Object);
        var aggregate2 = TestAggregateRoot.Create(_recorder.Object, _idFactory.Object);

        var result = aggregate1.Equals(aggregate2);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsWithSelf_ThenReturnsTrue()
    {
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
        var aggregate = TestAggregateRoot.Create(_recorder.Object, _idFactory.Object);

        // ReSharper disable once EqualExpressionComparison
        var result = aggregate.Equals(aggregate);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenRehydrateAndCreates_ThenReturnsInstance()
    {
        var result = TestAggregateRoot.Rehydrate()("anid".ToId(), _dependencyContainer.Object,
            new HydrationProperties());

        result.Id.Should().Be("anid".ToId());
    }

    [Fact]
    public void WhenRehydrate_ThenRaisesNoEvents()
    {
        var container = new Mock<IDependencyContainer>();
        container.Setup(c => c.Resolve<IRecorder>())
            .Returns(NullRecorder.Instance);
        container.Setup(c => c.Resolve<IIdentifierFactory>())
            .Returns(new NullIdentifierFactory());

        var created =
            TestAggregateRoot.Rehydrate()("anid".ToId(), container.Object,
                new HydrationProperties());

        created.GetChanges().Value.Should().BeEmpty();
        created.LastPersistedAtUtc.Should().BeNone();
        created.CreatedAtUtc.Should().Be(DateTime.MinValue);
        created.LastModifiedAtUtc.Should().Be(DateTime.MinValue);
    }

    [Fact]
    public void WhenChangeProperty_ThenRaisesEventAndModified()
    {
        _aggregate.ChangeProperty("achangedvalue");

        _aggregate.Events.Count.Should().Be(2);
        _aggregate.Events[0].Should().BeOfType<TestAggregateRoot.CreateEvent>();
        _aggregate.Events.Last().Should().BeEquivalentTo(new TestAggregateRoot.ChangeEvent
            { APropertyName = "achangedvalue" });
        _aggregate.LastModifiedAtUtc.Should().BeNear(DateTime.UtcNow);
    }

    [Fact]
    public void WhenGetChanges_ThenReturnsEventEntities()
    {
        _aggregate.ChangeProperty("avalue1");

        var result = _aggregate.GetChanges().Value;

        result.Count.Should().Be(2);
        result[0].EventType.Should().Be(nameof(TestAggregateRoot.CreateEvent));
        result[0].Version.Should().Be(1);
        result[0].Metadata.Should().Be(typeof(TestAggregateRoot.CreateEvent).AssemblyQualifiedName);
        result[1].EventType.Should().Be(nameof(TestAggregateRoot.ChangeEvent));
        result[1].Version.Should().Be(2);
        result[1].Metadata.Should().Be(typeof(TestAggregateRoot.ChangeEvent).AssemblyQualifiedName);
    }

    [Fact]
    public void WhenLoadChangesAgain_ThenReturnsError()
    {
        ((IEventingAggregateRoot)_aggregate).LoadChanges(new List<EventSourcedChangeEvent>
        {
            CreateEventEntity("aneventid1", 1)
        }, _typeMigrator);

        var result = ((IEventingAggregateRoot)_aggregate).LoadChanges(new List<EventSourcedChangeEvent>
        {
            CreateEventEntity("aneventid2", 2)
        }, _typeMigrator);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EventingAggregateRootBase_ChangesAlreadyLoaded);
    }

    [Fact]
    public void WhenLoadChanges_ThenSetsEventsAndUpdatesVersion()
    {
        ((IEventingAggregateRoot)_aggregate).LoadChanges(new List<EventSourcedChangeEvent>
        {
            CreateEventEntity("aneventid1", 1),
            CreateEventEntity("aneventid2", 2),
            CreateEventEntity("aneventid3", 3)
        }, _typeMigrator);

        _aggregate.EventStream.FirstEventVersion.Should().Be(1);
        _aggregate.EventStream.LastEventVersion.Should().Be(3);
    }

    [Fact]
    public void WhenLoadChangesWithOffsetVersions_ThenSetsEventsAndUpdatesVersion()
    {
        ((IEventingAggregateRoot)_aggregate).LoadChanges(new List<EventSourcedChangeEvent>
        {
            CreateEventEntity("aneventid1", 3),
            CreateEventEntity("aneventid2", 4),
            CreateEventEntity("aneventid3", 5)
        }, _typeMigrator);

        _aggregate.EventStream.FirstEventVersion.Should().Be(3);
        _aggregate.EventStream.LastEventVersion.Should().Be(5);
    }

    [Fact]
    public void WhenToEventAfterGetChanges_ThenReturnsOriginalEvent()
    {
        _aggregate.ChangeProperty("avalue");

        var entities = _aggregate.GetChanges().Value;

        var created = entities[0].ToEvent(_typeMigrator).Value;

        created.Should().BeOfType<TestAggregateRoot.CreateEvent>();
        created.As<TestAggregateRoot.CreateEvent>().RootId.Should().Be("anid");

        var changed = entities[1].ToEvent(_typeMigrator).Value;

        changed.Should().BeOfType<TestAggregateRoot.ChangeEvent>();
        changed.As<TestAggregateRoot.ChangeEvent>().APropertyName.Should().Be("avalue");
    }

    [Fact]
    public void WhenClearChanges_ThenResetsLastPersisted()
    {
        _aggregate.ClearChanges();

        _aggregate.LastPersistedAtUtc.Value.Should().BeNear(DateTime.UtcNow);
    }

    private static EventSourcedChangeEvent CreateEventEntity(string id, int version)
    {
        return new TestEvent { APropertyValue = "avalue" }
            .ToVersioned(new FixedIdentifierFactory(id), "anentitytype", version).Value;
    }
}