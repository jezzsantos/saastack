using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using QueryAny;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.Shared.IntegrationTests;

public abstract class AnyEventStoreBaseSpec
{
    private static int _entityIdCounter;
    private readonly EventStoreInfo _setup;

    protected AnyEventStoreBaseSpec(IEventStore eventStore)
    {
        _setup = new EventStoreInfo
        {
            Store = eventStore,
            ContainerName = $"{typeof(TestDataStoreEntity).GetEntityNameSafe()}"
        };
#if TESTINGONLY
        _setup.Store.DestroyAllAsync(_setup.ContainerName, CancellationToken.None);
#endif
    }

    [Fact]
    public async Task WhenDestroyAllWithNullEntityName_ThenThrows()
    {
#if TESTINGONLY
        await _setup.Store
            .Invoking(x => x.DestroyAllAsync(null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
#endif
    }

    [Fact]
    public async Task WhenGetEventStreamWithNullEntityName_ThenThrows()
    {
        var entityId = GetNextEntityId();
        await _setup.Store
            .Invoking(x => x.GetEventStreamAsync(null!, entityId, CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenGetEventStreamWithNullEntityId_ThenThrows()
    {
        await _setup.Store
            .Invoking(x =>
                x.GetEventStreamAsync(_setup.ContainerName, null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenGetEventStreamAndNoEventsInStream_ThenReturnsEmpty()
    {
        var entityId = GetNextEntityId();
        var result =
            await _setup.Store.GetEventStreamAsync(_setup.ContainerName, entityId,
                CancellationToken.None);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task WhenGetEventStreamAndWrongStream_ThenReturnsEmpty()
    {
        var entityId = GetNextEntityId();
        await _setup.Store.AddEventsAsync(_setup.ContainerName, entityId,
            [CreateEvent(1)], CancellationToken.None);

        var result = await _setup.Store.GetEventStreamAsync(_setup.ContainerName,
            "anotherentityid", CancellationToken.None);

        result.Value.Should().BeEmpty();
    }

    [Fact]
    public virtual async Task WhenGetEventStreamAndOneEventInStream_ThenReturnsEvent()
    {
        var entityId = GetNextEntityId();
        await _setup.Store.AddEventsAsync(_setup.ContainerName, entityId,
            [CreateEvent(1)], CancellationToken.None);

        var result =
            await _setup.Store.GetEventStreamAsync(_setup.ContainerName, entityId,
                CancellationToken.None);

        result.Value.Count.Should().Be(1);
        result.Value[0].Id.Should().Be("anid_v1");
        result.Value[0].Version.Should().Be(1);
        result.Value[0].EntityType.Should().Be("atype");
        result.Value[0].Data.FromJson<TestChangeEvent>()!.OccurredUtc.Should().BeNear(DateTime.UtcNow);
        result.Value[0].Metadata.Should().Be(typeof(TestChangeEvent).AssemblyQualifiedName);
        result.Value[0].EventType.Should().Be("TestChangeEvent");
    }

    [Fact]
    public async Task WhenGetEventStreamAndEventsInStream_ThenReturnsEventsInOrder()
    {
        var entityId = GetNextEntityId();
        await _setup.Store.AddEventsAsync(_setup.ContainerName, entityId,
        [
            CreateEvent(1),
            CreateEvent(2),
            CreateEvent(3)
        ], CancellationToken.None);

        var result =
            await _setup.Store.GetEventStreamAsync(_setup.ContainerName, entityId,
                CancellationToken.None);

        result.Value.Count.Should().Be(3);
        result.Value[0].Id.Should().Be("anid_v1");
        result.Value[1].Id.Should().Be("anid_v2");
        result.Value[2].Id.Should().Be("anid_v3");
    }

    [Fact]
    public async Task WhenAddEventsAndEntityNameIsNull_ThenThrows()
    {
        var entityId = GetNextEntityId();
        await _setup.Store
            .Invoking(x => x.AddEventsAsync(null!, entityId, [CreateEvent(3)],
                CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenAddEventsAndEntityIdIsNull_ThenThrows()
    {
        await _setup.Store
            .Invoking(x =>
                x.AddEventsAsync(_setup.ContainerName, null!,
                    [CreateEvent(3)], CancellationToken.None))
            .Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task WhenAddEventsAndEventsIsNull_ThenThrows()
    {
        var entityId = GetNextEntityId();
        await _setup.Store
            .Invoking(x => x.AddEventsAsync(_setup.ContainerName, entityId, null!, CancellationToken.None))
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task WhenAddEvents_ThenEventsAdded()
    {
        var entityId = GetNextEntityId();
        await _setup.Store.AddEventsAsync(_setup.ContainerName, entityId,
            [CreateEvent(1)], CancellationToken.None);

        var result =
            await _setup.Store.GetEventStreamAsync(_setup.ContainerName, entityId,
                CancellationToken.None);

        result.Value.Count.Should().Be(1);
        result.Value.Last().Id.Should().Be("anid_v1");
    }

    [Fact]
    public async Task WhenAddEventsAndStreamCleared_ThenReturnsError()
    {
        var entityId = GetNextEntityId();

        var result = await _setup.Store.AddEventsAsync(_setup.ContainerName, entityId,
            [CreateEvent(3)], CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityExists,
            Resources.EventStoreExtensions_ConcurrencyVerificationFailed_StreamReset
                .Format($"testentities_{entityId}"));
    }

    [Fact]
    public async Task WhenAddEventsAndNextEventReplaysPreviousVersion_ThenReturnsError()
    {
        var entityId = GetNextEntityId();
        await _setup.Store.AddEventsAsync(_setup.ContainerName, entityId,
        [
            CreateEvent(1),
            CreateEvent(2),
            CreateEvent(3)
        ], CancellationToken.None);

        var result = await _setup.Store.AddEventsAsync(_setup.ContainerName, entityId,
            [CreateEvent(1)], CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityExists,
            Resources.EventStoreExtensions_ConcurrencyVerificationFailed_StreamAlreadyUpdated.Format(
                $"testentities_{entityId}", 1));
    }

    [Fact]
    public async Task WhenAddEventsAndNextEventSkipsSomeVersions_ThenReturnsError()
    {
        var entityId = GetNextEntityId();
        await _setup.Store.AddEventsAsync(_setup.ContainerName, entityId,
        [
            CreateEvent(1),
            CreateEvent(2),
            CreateEvent(3)
        ], CancellationToken.None);

        var result = await _setup.Store.AddEventsAsync(_setup.ContainerName, entityId,
            [CreateEvent(10)], CancellationToken.None);

        result.Should().BeError(ErrorCode.EntityExists,
            Resources.EventStoreExtensions_ConcurrencyVerificationFailed_MissingUpdates.Format(
                $"testentities_{entityId}", 4, 10));
    }

    [Fact]
    public async Task WhenAddEventsAndNextEventsAreNextVersions_ThenAdds()
    {
        var entityId = GetNextEntityId();
        await _setup.Store.AddEventsAsync(_setup.ContainerName, entityId,
        [
            CreateEvent(1),
            CreateEvent(2),
            CreateEvent(3)
        ], CancellationToken.None);

        await _setup.Store.AddEventsAsync(_setup.ContainerName, entityId,
        [
            CreateEvent(4),
            CreateEvent(5),
            CreateEvent(6)
        ], CancellationToken.None);

        var result =
            await _setup.Store.GetEventStreamAsync(_setup.ContainerName, entityId,
                CancellationToken.None);

        result.Value.Count.Should().Be(6);
        result.Value.Last().Id.Should().Be("anid_v6");
    }

    public class EventStoreInfo
    {
        public required string ContainerName { get; set; }

        public required IEventStore Store { get; set; }
    }

    protected IEventStore EventStore => _setup.Store;

    protected string ContainerName => _setup.ContainerName;

    protected static string GetNextEntityId()
    {
        return $"anentityid{++_entityIdCounter}";
    }

    protected static EventSourcedChangeEvent CreateEvent(int version)
    {
        return new TestChangeEvent
        {
            RootId = "arootid",
            OccurredUtc = DateTime.UtcNow
        }.ToVersioned($"anid_v{version}".ToIdentifierFactory(), "atype", version).Value;
    }
}