using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Common.Projections;
using Infrastructure.Eventing.Interfaces.Projections;
using Moq;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Eventing.Common.UnitTests.Projections;

[Trait("Category", "Unit")]
public sealed class ReadModelProjectorSpec : IDisposable
{
    private readonly Mock<IReadModelCheckpointRepository> _checkpointRepository;
    private readonly Mock<IReadModelProjection> _projection;
    private readonly ReadModelProjector _projector;

    public ReadModelProjectorSpec()
    {
        var recorder = new Mock<IRecorder>();
        _checkpointRepository = new Mock<IReadModelCheckpointRepository>();
        var changeEventTypeMigrator = new ChangeEventTypeMigrator();
        _projection = new Mock<IReadModelProjection>();
        _projection.Setup(prj => prj.RootAggregateType)
            .Returns(typeof(string));
        _projection.Setup(prj => prj.ProjectEventAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<bool, Error>>(true));
        var projections = new List<IReadModelProjection> { _projection.Object };
        _projector = new ReadModelProjector(recorder.Object, _checkpointRepository.Object,
            changeEventTypeMigrator, projections.ToArray());
    }

    ~ReadModelProjectorSpec()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            _projector.Dispose();
        }
    }

    [Fact]
    public async Task WhenWriteEventStreamAsyncAndNoEvents_ThenReturns()
    {
        await _projector.WriteEventStreamAsync("astreamname", new List<EventStreamChangeEvent>(),
            CancellationToken.None);

        _checkpointRepository.Verify(cs => cs.LoadCheckpointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _projection.Verify(prj => prj.ProjectEventAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _checkpointRepository.Verify(
            cs => cs.SaveCheckpointAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenWriteEventStreamAsyncAndNoConfiguredProjection_ThenReturnsError()
    {
        _projection.Setup(prj => prj.RootAggregateType)
            .Returns(typeof(string));

        var result = await _projector.WriteEventStreamAsync("astreamname", new List<EventStreamChangeEvent>
        {
            new()
            {
                Data = null!,
                EntityType = "atypename",
                EventType = null!,
                Id = null!,
                LastPersistedAtUtc = default,
                Metadata = null!,
                StreamName = null!,
                Version = 0
            }
        }, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.ReadModelProjector_ProjectionNotConfigured.Format("atypename"));

        _checkpointRepository.Verify(cs => cs.LoadCheckpointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _projection.Verify(prj => prj.ProjectEventAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _checkpointRepository.Verify(
            cs => cs.SaveCheckpointAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenWriteEventStreamAsyncAndEventVersionGreaterThanCheckpoint_ThenReturnsError()
    {
        _checkpointRepository.Setup(cs => cs.LoadCheckpointAsync("astreamname", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<int, Error>>(5));

        _projection.Setup(prj => prj.RootAggregateType)
            .Returns(typeof(string));

        var result = await _projector.WriteEventStreamAsync("astreamname", new List<EventStreamChangeEvent>
        {
            new()
            {
                Data = null!,
                EntityType = nameof(String),
                EventType = null!,
                Id = null!,
                LastPersistedAtUtc = default,
                Metadata = null!,
                StreamName = null!,
                Version = 6
            }
        }, CancellationToken.None);
        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.ReadModelProjector_CheckpointError.Format("astreamname", 5, 6));
    }

    [Fact]
    public async Task WhenWriteEventStreamAsyncAndEventVersionLessThanCheckpoint_ThenSkipsPreviousVersions()
    {
        _checkpointRepository.Setup(cs => cs.LoadCheckpointAsync("astreamname", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<int, Error>>(5));

        await _projector.WriteEventStreamAsync("astreamname", new List<EventStreamChangeEvent>
        {
            new()
            {
                Id = "anid1",
                EntityType = nameof(String),
                Data = new TestEvent { RootId = "aneventid1" }.ToEventJson(),
                Version = 4,
                Metadata = new EventMetadata(typeof(TestEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            },
            new()
            {
                Id = "anid2",
                EntityType = nameof(String),
                Data = new TestEvent { RootId = "aneventid2" }.ToEventJson(),
                Version = 5,
                Metadata = new EventMetadata(typeof(TestEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            },
            new()
            {
                Id = "anid3",
                EntityType = nameof(String),
                Data = new TestEvent { RootId = "aneventid3" }.ToEventJson(),
                Version = 6,
                Metadata = new EventMetadata(typeof(TestEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            }
        }, CancellationToken.None);

        _checkpointRepository.Verify(cs => cs.LoadCheckpointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid1"
        ), It.IsAny<CancellationToken>()), Times.Never);
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid2"
        ), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid3"
        ), It.IsAny<CancellationToken>()));
        _checkpointRepository.Verify(cs => cs.SaveCheckpointAsync("astreamname", 7, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenWriteEventStreamAsyncAndDeserializationOfEventsFails_ThenReturnsError()
    {
        _checkpointRepository.Setup(cs => cs.LoadCheckpointAsync("astreamname", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<int, Error>>(3));

        var result = await _projector.WriteEventStreamAsync("astreamname", new List<EventStreamChangeEvent>
        {
            new()
            {
                Id = "anid",
                LastPersistedAtUtc = default,
                EntityType = nameof(String),
                EventType = null!,
                Data = new TestEvent
                {
                    RootId = "aneventid"
                }.ToEventJson(),
                Version = 3,
                Metadata = new EventMetadata("unknowntype"),
                StreamName = null!
            }
        }, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation);
    }

    [Fact]
    public async Task WhenWriteEventStreamAsyncAndFirstEverEvent_ThenProjectsEvents()
    {
        const int startingCheckpoint = ReadModelCheckpointRepository.StartingCheckpointVersion;
        _checkpointRepository.Setup(cs => cs.LoadCheckpointAsync("astreamname", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<int, Error>>(startingCheckpoint));

        await _projector.WriteEventStreamAsync("astreamname", new List<EventStreamChangeEvent>
        {
            new()
            {
                Id = "anid1",
                EntityType = nameof(String),
                Data = new TestEvent { RootId = "aneventid1" }.ToEventJson(),
                Version = startingCheckpoint,
                Metadata = new EventMetadata(typeof(TestEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            },
            new()
            {
                Id = "anid2",
                EntityType = nameof(String),
                Data = new TestEvent { RootId = "aneventid2" }.ToEventJson(),
                Version = startingCheckpoint + 1,
                Metadata = new EventMetadata(typeof(TestEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            },
            new()
            {
                Id = "anid3",
                EntityType = nameof(String),
                Data = new TestEvent { RootId = "aneventid3" }.ToEventJson(),
                Version = startingCheckpoint + 2,
                Metadata = new EventMetadata(typeof(TestEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            }
        }, CancellationToken.None);

        _checkpointRepository.Verify(cs => cs.LoadCheckpointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid1"
        ), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid2"
        ), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid3"
        ), It.IsAny<CancellationToken>()));
        _checkpointRepository.Verify(cs =>
            cs.SaveCheckpointAsync("astreamname", startingCheckpoint + 3, It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenWriteEventStreamAsyncAndEventNotHandledByProjection_ThenReturnsError()
    {
        _checkpointRepository.Setup(cs => cs.LoadCheckpointAsync("astreamname", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<int, Error>>(3));
        _projection.Setup(prj => prj.ProjectEventAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<bool, Error>>(false));

        var result = await _projector.WriteEventStreamAsync("astreamname", new List<EventStreamChangeEvent>
        {
            new()
            {
                Id = "anid1",
                EntityType = nameof(String),
                Data = new TestEvent { RootId = "aneventid1" }.ToEventJson(),
                Version = 3,
                Metadata = new EventMetadata(typeof(TestEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            }
        }, CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected,
            Resources.ReadModelProjector_ProjectionError.Format("IReadModelProjectionProxy", "anid1",
                typeof(TestEvent).AssemblyQualifiedName!));
        _checkpointRepository.Verify(cs => cs.LoadCheckpointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid1"
        ), It.IsAny<CancellationToken>()));
        _checkpointRepository.Verify(
            cs => cs.SaveCheckpointAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenWriteEventStreamAsync_ThenProjectsEvents()
    {
        _checkpointRepository.Setup(cs => cs.LoadCheckpointAsync("astreamname", It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<int, Error>>(3));

        await _projector.WriteEventStreamAsync("astreamname", new List<EventStreamChangeEvent>
        {
            new()
            {
                Id = "anid1",
                EntityType = nameof(String),
                Data = new TestEvent { RootId = "aneventid1" }.ToEventJson(),
                Version = 3,
                Metadata = new EventMetadata(typeof(TestEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            },
            new()
            {
                Id = "anid2",
                EntityType = nameof(String),
                Data = new TestEvent { RootId = "aneventid2" }.ToEventJson(),
                Version = 4,
                Metadata = new EventMetadata(typeof(TestEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            },
            new()
            {
                Id = "anid3",
                EntityType = nameof(String),
                Data = new TestEvent { RootId = "aneventid3" }.ToEventJson(),
                Version = 5,
                Metadata = new EventMetadata(typeof(TestEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            }
        }, CancellationToken.None);

        _checkpointRepository.Verify(cs => cs.LoadCheckpointAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid1"
        ), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid2"
        ), It.IsAny<CancellationToken>()));
        _projection.Verify(prj => prj.ProjectEventAsync(It.Is<TestEvent>(e =>
            e.RootId == "aneventid3"
        ), It.IsAny<CancellationToken>()));
        _checkpointRepository.Verify(cs => cs.SaveCheckpointAsync("astreamname", 6, It.IsAny<CancellationToken>()));
    }
}