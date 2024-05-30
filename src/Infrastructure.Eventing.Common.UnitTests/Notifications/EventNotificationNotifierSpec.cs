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
    private readonly Mock<IDomainEventConsumerRelay> _consumerRelay;
    private readonly Mock<IEventNotificationMessageBroker> _messageBroker;
    private readonly EventNotificationNotifier _notifier;
    private readonly Mock<IEventNotificationRegistration> _registration;

    public EventNotificationNotifierSpec()
    {
        var recorder = new Mock<IRecorder>();
        var changeEventTypeMigrator = new ChangeEventTypeMigrator();
        _registration = new Mock<IEventNotificationRegistration>();
        _registration.Setup(reg => reg.IntegrationEventTranslator.RootAggregateType)
            .Returns(typeof(string));
        _registration.Setup(p =>
                p.IntegrationEventTranslator.TranslateAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Optional<IIntegrationEvent>.None);
        _consumerRelay = new Mock<IDomainEventConsumerRelay>();
        _messageBroker = new Mock<IEventNotificationMessageBroker>();
        _messageBroker.Setup(mb => mb.PublishAsync(It.IsAny<IIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
        var registrations = new List<IEventNotificationRegistration> { _registration.Object };
        _notifier = new EventNotificationNotifier(recorder.Object, changeEventTypeMigrator, registrations,
            _consumerRelay.Object, _messageBroker.Object);
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
        await _notifier.WriteEventStreamAsync("astreamname", [],
            CancellationToken.None);

        _consumerRelay.Verify(
            cr => cr.RelayDomainEventAsync(It.IsAny<IDomainEvent>(), It.IsAny<EventStreamChangeEvent>(),
                It.IsAny<CancellationToken>()), Times.Never);
        _messageBroker.Verify(mb => mb.PublishAsync(It.IsAny<IIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _registration.Verify(
            reg => reg.IntegrationEventTranslator.TranslateAsync(It.IsAny<IDomainEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenWriteEventStreamAndDeserializationOfEventsFails_ThenReturnsError()
    {
        var result = await _notifier.WriteEventStreamAsync("astreamname", [
            new EventStreamChangeEvent
            {
                Id = "anid",
                RootAggregateType = nameof(String),
                Data = new TestDomainEvent
                {
                    RootId = "aneventid"
                }.ToEventJson(),
                Version = 1,
                Metadata = new EventMetadata("unknowntype"),
                EventType = null!,
                LastPersistedAtUtc = default,
                StreamName = null!
            }
        ], CancellationToken.None);

        result.Should().BeError(ErrorCode.RuleViolation);
    }

    [Fact]
    public async Task WhenWriteEventStreamAndNoRegistrations_ThenNotifiesDomainEvent()
    {
        _registration.Setup(reg => reg.IntegrationEventTranslator.RootAggregateType)
            .Returns(typeof(string));
        var changeEvent = new EventStreamChangeEvent
        {
            Id = "anid1",
            RootAggregateType = nameof(String),
            Data = new TestDomainEvent { RootId = "aneventid" }.ToEventJson(),
            Version = 0,
            Metadata = new EventMetadata(typeof(TestDomainEvent).AssemblyQualifiedName!),
            EventType = null!,
            LastPersistedAtUtc = default,
            StreamName = null!
        };

        var result = await _notifier.WriteEventStreamAsync("astreamname", [changeEvent], CancellationToken.None);

        result.Should().BeSuccess();
        _registration.Verify(reg => reg.IntegrationEventTranslator.TranslateAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "aneventid"
        ), It.IsAny<CancellationToken>()));
        _consumerRelay.Verify(cr => cr.RelayDomainEventAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "aneventid"
        ), changeEvent, It.IsAny<CancellationToken>()));
        _messageBroker.Verify(mb => mb.PublishAsync(It.IsAny<IIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenWriteEventStreamAndRegisteredTranslatorDoesNotTranslateEvent_ThenOnlyNotifiesDomainEvent()
    {
        _registration.Setup(p =>
                p.IntegrationEventTranslator.TranslateAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDomainEvent _, CancellationToken _) => Optional<IIntegrationEvent>.None);
        var changeEvent = new EventStreamChangeEvent
        {
            Id = "anid1",
            RootAggregateType = nameof(String),
            Data = new TestDomainEvent { RootId = "aneventid" }.ToEventJson(),
            Version = 0,
            Metadata = new EventMetadata(typeof(TestDomainEvent).AssemblyQualifiedName!),
            EventType = null!,
            LastPersistedAtUtc = default,
            StreamName = null!
        };

        var result = await _notifier.WriteEventStreamAsync("astreamname", [
            changeEvent
        ], CancellationToken.None);

        result.Should().BeSuccess();
        _registration.Verify(reg => reg.IntegrationEventTranslator.TranslateAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "aneventid"
        ), It.IsAny<CancellationToken>()));
        _consumerRelay.Verify(cr => cr.RelayDomainEventAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "aneventid"
        ), changeEvent, It.IsAny<CancellationToken>()));
        _messageBroker.Verify(mb => mb.PublishAsync(It.IsAny<IIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenWriteEventStreamWithSingleEvent_ThenNotifiesBothDomainAndIntegrationEvents()
    {
        _registration.Setup(p =>
                p.IntegrationEventTranslator.TranslateAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDomainEvent domainEvent, CancellationToken _) =>
                new TestIntegrationEvent(domainEvent.RootId).ToOptional<IIntegrationEvent>());
        var changeEvent = new EventStreamChangeEvent
        {
            Id = "anid1",
            RootAggregateType = nameof(String),
            Data = new TestDomainEvent { RootId = "aneventid" }.ToEventJson(),
            Version = 0,
            Metadata = new EventMetadata(typeof(TestDomainEvent).AssemblyQualifiedName!),
            EventType = null!,
            LastPersistedAtUtc = default,
            StreamName = null!
        };

        var result = await _notifier.WriteEventStreamAsync("astreamname", [
            changeEvent
        ], CancellationToken.None);

        result.Should().BeSuccess();
        _registration.Verify(reg => reg.IntegrationEventTranslator.TranslateAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "aneventid"
        ), It.IsAny<CancellationToken>()));
        _consumerRelay.Verify(cr => cr.RelayDomainEventAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "aneventid"
        ), changeEvent, It.IsAny<CancellationToken>()));
        _messageBroker.Verify(mb => mb.PublishAsync(It.Is<TestIntegrationEvent>(ie =>
            ie.RootId == "aneventid"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenWriteEventStreamWithMultipleEvents_ThenNotifiesBothDomainAndIntegrationEvents()
    {
        _registration.Setup(p =>
                p.IntegrationEventTranslator.TranslateAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDomainEvent domainEvent, CancellationToken _) =>
                new TestIntegrationEvent(domainEvent.RootId).ToOptional<IIntegrationEvent>());
        var changeEvent1 = new EventStreamChangeEvent
        {
            Id = "anid1",
            RootAggregateType = nameof(String),
            Data = new TestDomainEvent { RootId = "aneventid1" }.ToEventJson(),
            Version = 0,
            Metadata = new EventMetadata(typeof(TestDomainEvent).AssemblyQualifiedName!),
            EventType = null!,
            LastPersistedAtUtc = default,
            StreamName = null!
        };
        var changeEvent2 = new EventStreamChangeEvent
        {
            Id = "anid2",
            RootAggregateType = nameof(String),
            Data = new TestDomainEvent { RootId = "aneventid2" }.ToEventJson(),
            Version = 1,
            Metadata = new EventMetadata(typeof(TestDomainEvent).AssemblyQualifiedName!),
            EventType = null!,
            LastPersistedAtUtc = default,
            StreamName = null!
        };
        var changeEvent3 = new EventStreamChangeEvent
        {
            Id = "anid3",
            RootAggregateType = nameof(String),
            Data = new TestDomainEvent { RootId = "aneventid3" }.ToEventJson(),
            Version = 2,
            Metadata = new EventMetadata(typeof(TestDomainEvent).AssemblyQualifiedName!),
            EventType = null!,
            LastPersistedAtUtc = default,
            StreamName = null!
        };

        var result = await _notifier.WriteEventStreamAsync("astreamname", [changeEvent1, changeEvent2, changeEvent3],
            CancellationToken.None);

        result.Should().BeSuccess();
        _registration.Verify(reg => reg.IntegrationEventTranslator.TranslateAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "aneventid1"
        ), It.IsAny<CancellationToken>()));
        _consumerRelay.Verify(cr => cr.RelayDomainEventAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "aneventid1"
        ), changeEvent1, It.IsAny<CancellationToken>()));
        _messageBroker.Verify(mb => mb.PublishAsync(It.Is<TestIntegrationEvent>(e =>
            e.RootId == "aneventid1"
        ), It.IsAny<CancellationToken>()));
        _registration.Verify(reg => reg.IntegrationEventTranslator.TranslateAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "aneventid2"
        ), It.IsAny<CancellationToken>()));
        _consumerRelay.Verify(cr => cr.RelayDomainEventAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "aneventid2"
        ), changeEvent2, It.IsAny<CancellationToken>()));
        _messageBroker.Verify(mb => mb.PublishAsync(It.Is<TestIntegrationEvent>(e =>
            e.RootId == "aneventid2"
        ), It.IsAny<CancellationToken>()));
        _registration.Verify(r => r.IntegrationEventTranslator.TranslateAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "aneventid3"
        ), It.IsAny<CancellationToken>()));
        _consumerRelay.Verify(cr => cr.RelayDomainEventAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "aneventid3"
        ), changeEvent3, It.IsAny<CancellationToken>()));
        _messageBroker.Verify(mb => mb.PublishAsync(It.Is<TestIntegrationEvent>(e =>
            e.RootId == "aneventid3"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenWriteEventStreamAndConsumerRelayReturnsError_ThenStopsAndReturnsError()
    {
        _registration.Setup(p =>
                p.IntegrationEventTranslator.TranslateAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDomainEvent domainEvent, CancellationToken _) =>
                new TestIntegrationEvent(domainEvent.RootId).ToOptional<IIntegrationEvent>());
        _consumerRelay.Setup(cr => cr.RelayDomainEventAsync(It.IsAny<IDomainEvent>(),
                It.IsAny<EventStreamChangeEvent>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.RuleViolation("amessage"));
        var changeEvent = new EventStreamChangeEvent
        {
            Id = "anid1",
            RootAggregateType = nameof(String),
            Data = new TestDomainEvent { RootId = "aneventid" }.ToEventJson(),
            Version = 0,
            Metadata = new EventMetadata(typeof(TestDomainEvent).AssemblyQualifiedName!),
            EventType = null!,
            LastPersistedAtUtc = default,
            StreamName = null!
        };

        var result = await _notifier.WriteEventStreamAsync("astreamname", [changeEvent], CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected, Error.RuleViolation("amessage")
            .Wrap(Resources.EventNotificationNotifier_ConsumerError.Format("IDomainEventConsumerRelayProxy",
                "aneventid", typeof(TestDomainEvent).AssemblyQualifiedName!)).ToString());
        _registration.Verify(
            reg => reg.IntegrationEventTranslator.TranslateAsync(It.IsAny<IDomainEvent>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        _consumerRelay.Verify(cr => cr.RelayDomainEventAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "aneventid"
        ), changeEvent, It.IsAny<CancellationToken>()));
        _messageBroker.Verify(mb => mb.PublishAsync(It.IsAny<TestIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenWriteEventStreamAndIntegrationTranslatorReturnsError_ThenStopsAndReturnsError()
    {
        _registration.Setup(p =>
                p.IntegrationEventTranslator.TranslateAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDomainEvent domainEvent, CancellationToken _) =>
                new TestIntegrationEvent(domainEvent.RootId).ToOptional<IIntegrationEvent>());
        _registration.Setup(reg =>
                reg.IntegrationEventTranslator.TranslateAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.RuleViolation("amessage"));
        var changeEvent = new EventStreamChangeEvent
        {
            Id = "anid1",
            RootAggregateType = nameof(String),
            Data = new TestDomainEvent { RootId = "aneventid" }.ToEventJson(),
            Version = 0,
            Metadata = new EventMetadata(typeof(TestDomainEvent).AssemblyQualifiedName!),
            EventType = null!,
            LastPersistedAtUtc = default,
            StreamName = null!
        };

        var result = await _notifier.WriteEventStreamAsync("astreamname", [changeEvent], CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected, Error.RuleViolation("amessage")
            .Wrap(Resources.EventNotificationNotifier_TranslatorError.Format(
                "IIntegrationEventNotificationTranslatorProxy",
                "aneventid", typeof(TestDomainEvent).AssemblyQualifiedName!)).ToString());
        _registration.Verify(
            reg => reg.IntegrationEventTranslator.TranslateAsync(It.IsAny<IDomainEvent>(),
                It.IsAny<CancellationToken>()));
        _consumerRelay.Verify(cr => cr.RelayDomainEventAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "aneventid"), changeEvent, It.IsAny<CancellationToken>()));
        _messageBroker.Verify(mb => mb.PublishAsync(It.IsAny<TestIntegrationEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenWriteEventStreamAndMessageBrokerReturnsError_ThenStopsAndReturnsError()
    {
        _registration.Setup(p =>
                p.IntegrationEventTranslator.TranslateAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IDomainEvent domainEvent, CancellationToken _) =>
                new TestIntegrationEvent(domainEvent.RootId).ToOptional<IIntegrationEvent>());
        _messageBroker.Setup(mb => mb.PublishAsync(It.IsAny<IIntegrationEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.RuleViolation("amessage"));
        var changeEvent = new EventStreamChangeEvent
        {
            Id = "anid1",
            RootAggregateType = nameof(String),
            Data = new TestDomainEvent { RootId = "aneventid" }.ToEventJson(),
            Version = 0,
            Metadata = new EventMetadata(typeof(TestDomainEvent).AssemblyQualifiedName!),
            EventType = null!,
            LastPersistedAtUtc = default,
            StreamName = null!
        };

        var result = await _notifier.WriteEventStreamAsync("astreamname", [changeEvent], CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected, Error.RuleViolation("amessage")
            .Wrap(Resources.EventNotificationNotifier_MessageBrokerError.Format("IEventNotificationMessageBrokerProxy",
                "aneventid", typeof(TestDomainEvent).AssemblyQualifiedName!)).ToString());
        _registration.Verify(
            reg => reg.IntegrationEventTranslator.TranslateAsync(It.IsAny<IDomainEvent>(),
                It.IsAny<CancellationToken>()));
        _consumerRelay.Verify(cr => cr.RelayDomainEventAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "aneventid"), changeEvent, It.IsAny<CancellationToken>()));
        _messageBroker.Verify(mb => mb.PublishAsync(It.IsAny<TestIntegrationEvent>(), It.IsAny<CancellationToken>()));
    }
}