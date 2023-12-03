using Application.Persistence.Interfaces;
using Common;
using Domain.Common.Events;
using Domain.Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Infrastructure.Persistence.Interfaces;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Persistence.Common.UnitTests;

[Trait("Category", "Unit")]
public class EventSourcingDddCommandStoreSpec
{
    private readonly Mock<IDomainFactory> _domainFactory;
    private readonly Mock<IEventStore> _eventStore;
    private readonly EventSourcingDddCommandStore<TestEventingAggregateRoot> _store;
    private EventStreamChangedArgs? _eventStreamChangedArgs;

    public EventSourcingDddCommandStoreSpec()
    {
        var recorder = new Mock<IRecorder>();
        _domainFactory = new Mock<IDomainFactory>();
        var migrator = new Mock<IEventSourcedChangeEventMigrator>();
        _eventStore = new Mock<IEventStore>();
        _store =
            new EventSourcingDddCommandStore<TestEventingAggregateRoot>(recorder.Object, _domainFactory.Object,
                migrator.Object, _eventStore.Object);

        _eventStreamChangedArgs = null;
        _store.OnEventStreamChanged += (_, args, _) => { _eventStreamChangedArgs = args; };
    }

    [Fact]
    public async Task WhenDestroyAll_ThenDestroysAllInStore()
    {
        await _store.DestroyAllAsync(CancellationToken.None);

        _eventStore.Verify(store => store.DestroyAllAsync("acontainername", CancellationToken.None));
    }

    [Fact]
    public async Task WhenLoadAndNoEventsFound_ThenReturnsError()
    {
        var aggregate = new TestEventingAggregateRoot("anid".ToId());
        _eventStore.Setup(store =>
                store.GetEventStreamAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<IReadOnlyList<EventSourcedChangeEvent>, Error>>(
                new List<EventSourcedChangeEvent>()));
        _domainFactory.Setup(df =>
                df.RehydrateAggregateRoot(It.IsAny<Type>(), It.IsAny<HydrationProperties>()))
            .Returns(aggregate);

        var result = await _store.LoadAsync("anid".ToId(), CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound);
        _eventStore.Verify(store =>
            store.GetEventStreamAsync("acontainername", "anid", CancellationToken.None));
        _domainFactory.Verify(df => df.RehydrateAggregateRoot(typeof(TestEventingAggregateRoot),
            It.IsAny<HydrationProperties>()), Times.Never);
    }

    [Fact]
    public async Task WhenLoadAndEventsFound_ThenReturnsNewEntityWithEvents()
    {
        var lastPersisted = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(1));
        var aggregate = new TestEventingAggregateRoot("anid".ToId());
        var events = new List<EventSourcedChangeEvent>
        {
            CreateEventEntity("aneventid1", 1, DateTime.MinValue),
            CreateEventEntity("aneventid2", 2, DateTime.MinValue),
            CreateEventEntity("aneventid3", 3, lastPersisted)
        };
        _eventStore.Setup(store =>
                store.GetEventStreamAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<IReadOnlyList<EventSourcedChangeEvent>, Error>>(events));
        _domainFactory.Setup(df =>
                df.RehydrateAggregateRoot(It.IsAny<Type>(), It.IsAny<HydrationProperties>()))
            .Returns(aggregate);
        _domainFactory.Setup(df => df.RehydrateValueObject(typeof(Identifier), It.IsAny<string>()))
            .Returns((Type _, string value) => value.ToId());
        _domainFactory.Setup(df => df.RehydrateValueObject(typeof(EventMetadata), It.IsAny<string>()))
            .Returns((Type _, string value) => new EventMetadata(value));

        var result = await _store.LoadAsync("anid".ToId(), CancellationToken.None);

        result.Value.Should().Be(aggregate);
        result.Value.LoadedChangeEvents.Should().BeEquivalentTo(events);
        _eventStore.Verify(store =>
            store.GetEventStreamAsync("acontainername", "anid", CancellationToken.None));
        _domainFactory.Verify(df => df.RehydrateAggregateRoot(typeof(TestEventingAggregateRoot),
            It.Is<HydrationProperties>(dic =>
                dic.Count == 2
                && dic[nameof(CommandEntity.Id)].Value.As<Identifier>() == "anid".ToId()
                && dic[nameof(CommandEntity.LastPersistedAtUtc)].Value.As<DateTime>() == lastPersisted
            )));
    }

    [Fact]
    public async Task WhenLoadAndPreviouslyTombstoned_ThenReturnsError()
    {
        var events = new List<EventSourcedChangeEvent>
        {
            CreateEventEntity("aneventid1", 1, DateTime.MinValue),
            CreateEventEntity("aneventid2", 2, DateTime.MinValue),
            CreateEventEntity("aneventid3", 3, DateTime.UtcNow),
            CreateTombstoneEntity("aneventid4", 4)
        };
        _eventStore.Setup(store =>
                store.GetEventStreamAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<IReadOnlyList<EventSourcedChangeEvent>, Error>>(events));

        var result = await _store.LoadAsync("anid".ToId(), CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityNotFound, Resources.IEventSourcingDddCommandStore_StreamTombstoned);
    }

    [Fact]
    public async Task WhenSaveAndAggregateHasNoIdentifier_ThenReturnsError()
    {
        var result =
            await _store.SaveAsync(new TestEventingAggregateRoot(Identifier.Empty()), CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityExists,
            Resources.IEventSourcingDddCommandStore_SaveWithAggregateIdMissing);
    }

    [Fact]
    public async Task WhenSaveAndHasNoEventsWithNoNewEvents_ThenDoesNothing()
    {
        var aggregate = new TestEventingAggregateRoot("anid".ToId());

        await _store.SaveAsync(aggregate, CancellationToken.None);

        aggregate.ClearedChanges.Should().BeFalse();
        _eventStreamChangedArgs.Should().BeNull();
    }

    [Fact]
    public async Task WhenSaveAndHasOneEventWithNewEvents_ThenSavesAndPublishesNewEvents()
    {
        var event1 = CreateEventEntity("aneventid1", 1, DateTime.UtcNow);
        var aggregate = new TestEventingAggregateRoot("anid".ToId())
        {
            ChangeEvents = new List<EventSourcedChangeEvent>
            {
                CreateEventEntity("aneventid2", 2, DateTime.UtcNow),
                CreateEventEntity("aneventid3", 3, DateTime.UtcNow)
            },
            LoadedChangeEvents = new List<EventSourcedChangeEvent>
            {
                event1
            }
        };
        _eventStore.Setup(store => store.AddEventsAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<List<EventSourcedChangeEvent>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<string, Error>>("astreamname"));

        await _store.SaveAsync(aggregate, CancellationToken.None);

        _eventStore.Verify(
            store => store.AddEventsAsync("acontainername", "anid", It.Is<List<EventSourcedChangeEvent>>(vce =>
                vce.Count == 2
                && vce[0].Id == "aneventid2"
                && vce[1].Id == "aneventid3"
            ), CancellationToken.None));
        aggregate.ClearedChanges.Should().BeTrue();
        _eventStreamChangedArgs!.Events[0].Id.Should().Be("aneventid2");
        _eventStreamChangedArgs!.Events[1].Id.Should().Be("aneventid3");
    }

    private static EventSourcedChangeEvent CreateEventEntity(string id, int version, DateTime lastPersisted)
    {
        var versioned = new TestEvent()
            .ToVersioned(new FixedIdentifierFactory(id), "anentitytype", version);
        var eventSourcedChangeEvent = versioned.Value;
        eventSourcedChangeEvent.LastPersistedAtUtc = lastPersisted;

        return eventSourcedChangeEvent;
    }

    private static EventSourcedChangeEvent CreateTombstoneEntity(string id, int version)
    {
        var versioned = Global.StreamDeleted.Create(id.ToId(), id.ToId())
            .ToVersioned(new FixedIdentifierFactory(id), "anentitytype", version);
        var eventSourcedChangeEvent = versioned.Value;
        eventSourcedChangeEvent.LastPersistedAtUtc = DateTime.UtcNow;

        return eventSourcedChangeEvent;
    }
}