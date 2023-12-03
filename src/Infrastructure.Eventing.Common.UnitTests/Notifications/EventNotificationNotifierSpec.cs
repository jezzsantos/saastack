using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Common.Notifications;
using Infrastructure.Eventing.Interfaces.Notifications;
using Moq;
using UnitTesting.Common;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Eventing.Common.UnitTests.Notifications;

[Trait("Category", "Unit")]
public sealed class EventNotificationNotifierSpec : IDisposable
{
    private readonly EventNotificationNotifier _notifier;
    private readonly Mock<IEventNotificationRegistration> _registration;

    public EventNotificationNotifierSpec()
    {
        var recorder = new Mock<IRecorder>();
        var changeEventTypeMigrator = new ChangeEventTypeMigrator();
        _registration = new Mock<IEventNotificationRegistration>();
        _registration.Setup(p => p.Producer.RootAggregateType)
            .Returns(typeof(string));
        _registration.Setup(p => p.Producer.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<Optional<IDomainEvent>, Error>>(Optional<IDomainEvent>.None));
        _registration.Setup(p => p.Consumer.NotifyAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<bool, Error>>(true));
        var registrations = new List<IEventNotificationRegistration> { _registration.Object };
        _notifier = new EventNotificationNotifier(recorder.Object, changeEventTypeMigrator, registrations.ToArray());
    }

    ~EventNotificationNotifierSpec()
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
            _notifier.Dispose();
        }
    }

    [Fact]
    public async Task WhenWriteEventStreamAndNoEvents_ThenReturns()
    {
        await _notifier.WriteEventStreamAsync("astreamname", new List<EventStreamChangeEvent>(),
            CancellationToken.None);

        _registration.Verify(p => p.Producer.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenWriteEventStreamAndNoConfiguredConsumer_ThenReturns()
    {
        _registration.Setup(p => p.Producer.RootAggregateType)
            .Returns(typeof(string));

        var result = await _notifier.WriteEventStreamAsync("astreamname", new List<EventStreamChangeEvent>
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

        result.Should().BeSuccess();
        _registration.Verify(p => p.Producer.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenWriteEventStreamAndDeserializationOfEventsFails_ThenReturnsError()
    {
        var result = await _notifier.WriteEventStreamAsync("astreamname", new List<EventStreamChangeEvent>
        {
            new()
            {
                Id = "anid",
                EntityType = nameof(String),
                Data = new TestChangeEvent
                {
                    RootId = "aneventid"
                }.ToEventJson(),
                Version = 1,
                Metadata = new EventMetadata("unknowntype"),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            }
        }, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation);
    }

    [Fact]
    public async Task WhenWriteEventStreamAndProducerDoesNotPublishEvent_ThenReturns()
    {
        _registration.Setup(p => p.Producer.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns((IDomainEvent _, CancellationToken _) =>
                Task.FromResult<Result<Optional<IDomainEvent>, Error>>(Optional<IDomainEvent>.None));

        var result = await _notifier.WriteEventStreamAsync("astreamname", new List<EventStreamChangeEvent>
        {
            new()
            {
                Id = "anid1",
                EntityType = nameof(String),
                Data = new TestChangeEvent { RootId = "aneventid1" }.ToEventJson(),
                Version = 0,
                Metadata = new EventMetadata(typeof(TestChangeEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            }
        }, CancellationToken.None);

        result.Should().BeSuccess();
        _registration.Verify(p => p.Producer.PublishAsync(It.Is<TestChangeEvent>(e =>
            e.RootId == "aneventid1"
        ), It.IsAny<CancellationToken>()));
        _registration.Verify(p => p.Consumer.NotifyAsync(It.IsAny<TestChangeEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenWriteEventStreamAndFirstEverEvent_ThenNotifiesEvents()
    {
        _registration.Setup(p => p.Producer.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns((IDomainEvent @event, CancellationToken _) =>
                Task.FromResult<Result<Optional<IDomainEvent>, Error>>(new TestChangeEvent { RootId = @event.RootId }
                    .ToOptional<IDomainEvent>()));

        var result = await _notifier.WriteEventStreamAsync("astreamname", new List<EventStreamChangeEvent>
        {
            new()
            {
                Id = "anid1",
                EntityType = nameof(String),
                Data = new TestChangeEvent { RootId = "aneventid1" }.ToEventJson(),
                Version = 0,
                Metadata = new EventMetadata(typeof(TestChangeEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            },
            new()
            {
                Id = "anid2",
                EntityType = nameof(String),
                Data = new TestChangeEvent { RootId = "aneventid2" }.ToEventJson(),
                Version = 1,
                Metadata = new EventMetadata(typeof(TestChangeEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            },
            new()
            {
                Id = "anid3",
                EntityType = nameof(String),
                Data = new TestChangeEvent { RootId = "aneventid3" }.ToEventJson(),
                Version = 2,
                Metadata = new EventMetadata(typeof(TestChangeEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            }
        }, CancellationToken.None);

        result.Should().BeSuccess();
        _registration.Verify(p => p.Producer.PublishAsync(It.Is<TestChangeEvent>(e =>
            e.RootId == "aneventid2"
        ), It.IsAny<CancellationToken>()));
        _registration.Verify(p => p.Consumer.NotifyAsync(It.Is<TestChangeEvent>(e =>
            e.RootId == "aneventid2"
        ), It.IsAny<CancellationToken>()));
        _registration.Verify(p => p.Producer.PublishAsync(It.Is<TestChangeEvent>(e =>
            e.RootId == "aneventid3"
        ), It.IsAny<CancellationToken>()));
        _registration.Verify(p => p.Consumer.NotifyAsync(It.Is<TestChangeEvent>(e =>
            e.RootId == "aneventid3"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenWriteEventStreamAndConsumerDoesNotHandleEvent_ThenReturnsError()
    {
        _registration.Setup(p => p.Producer.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns((IDomainEvent @event, CancellationToken _) =>
                Task.FromResult<Result<Optional<IDomainEvent>, Error>>(new TestChangeEvent { RootId = @event.RootId }
                    .ToOptional<IDomainEvent>()));
        _registration.Setup(p => p.Consumer.NotifyAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<Result<bool, Error>>(false));

        var result = await _notifier.WriteEventStreamAsync("astreamname", new List<EventStreamChangeEvent>
        {
            new()
            {
                Id = "anid1",
                EntityType = nameof(String),
                Data = new TestChangeEvent { RootId = "aneventid1" }.ToEventJson(),
                Version = 0,
                Metadata = new EventMetadata(typeof(TestChangeEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            }
        }, CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.EventNotificationNotifier_ConsumerError.Format("IEventNotificationConsumerProxy", "anid1",
                typeof(TestChangeEvent).AssemblyQualifiedName!));
        _registration.Verify(p => p.Producer.PublishAsync(It.Is<TestChangeEvent>(e =>
            e.RootId == "aneventid1"
        ), It.IsAny<CancellationToken>()));
        _registration.Verify(p => p.Consumer.NotifyAsync(It.Is<TestChangeEvent>(e =>
            e.RootId == "aneventid1"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenWriteEventStream_ThenNotifiesEvents()
    {
        _registration.Setup(p => p.Producer.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns((IDomainEvent @event, CancellationToken _) =>
                Task.FromResult<Result<Optional<IDomainEvent>, Error>>(new TestChangeEvent { RootId = @event.RootId }
                    .ToOptional<IDomainEvent>()));

        var result = await _notifier.WriteEventStreamAsync("astreamname", new List<EventStreamChangeEvent>
        {
            new()
            {
                Id = "anid1",
                EntityType = nameof(String),
                Data = new TestChangeEvent { RootId = "aneventid1" }.ToEventJson(),
                Version = 3,
                Metadata = new EventMetadata(typeof(TestChangeEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            },
            new()
            {
                Id = "anid2",
                EntityType = nameof(String),
                Data = new TestChangeEvent { RootId = "aneventid2" }.ToEventJson(),
                Version = 4,
                Metadata = new EventMetadata(typeof(TestChangeEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            },
            new()
            {
                Id = "anid3",
                EntityType = nameof(String),
                Data = new TestChangeEvent { RootId = "aneventid3" }.ToEventJson(),
                Version = 5,
                Metadata = new EventMetadata(typeof(TestChangeEvent).AssemblyQualifiedName!),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            }
        }, CancellationToken.None);

        result.Should().BeSuccess();
        _registration.Verify(p => p.Producer.PublishAsync(It.Is<TestChangeEvent>(e =>
            e.RootId == "aneventid1"
        ), It.IsAny<CancellationToken>()));
        _registration.Verify(p => p.Consumer.NotifyAsync(It.Is<TestChangeEvent>(e =>
            e.RootId == "aneventid1"
        ), It.IsAny<CancellationToken>()));
        _registration.Verify(p => p.Producer.PublishAsync(It.Is<TestChangeEvent>(e =>
            e.RootId == "aneventid2"
        ), It.IsAny<CancellationToken>()));
        _registration.Verify(p => p.Consumer.NotifyAsync(It.Is<TestChangeEvent>(e =>
            e.RootId == "aneventid2"
        ), It.IsAny<CancellationToken>()));
        _registration.Verify(p => p.Producer.PublishAsync(It.Is<TestChangeEvent>(e =>
            e.RootId == "aneventid3"
        ), It.IsAny<CancellationToken>()));
        _registration.Verify(p => p.Consumer.NotifyAsync(It.Is<TestChangeEvent>(e =>
            e.RootId == "aneventid3"
        ), It.IsAny<CancellationToken>()));
    }
}