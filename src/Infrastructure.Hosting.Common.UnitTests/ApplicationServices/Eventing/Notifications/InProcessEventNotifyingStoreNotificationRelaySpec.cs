using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Infrastructure.Eventing.Common.Notifications;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Hosting.Common.ApplicationServices.Eventing.Notifications;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using Moq;
using Xunit;

namespace Infrastructure.Hosting.Common.UnitTests.ApplicationServices.Eventing.Notifications;

[Trait("Category", "Unit")]
public class InProcessEventNotifyingStoreNotificationRelaySpec
{
    private readonly TestConsumerRelay _consumerRelay;
    private readonly EventSourcingDddCommandStore<TestEventingAggregateRoot> _eventSourcingStore;
    private readonly TestMessageBroker _messageBroker;
    private readonly InProcessEventNotifyingStoreNotificationRelay _relay;

    public InProcessEventNotifyingStoreNotificationRelaySpec()
    {
        var recorder = new Mock<IRecorder>();
        var migrator = new Mock<IEventSourcedChangeEventMigrator>();
        migrator.Setup(m => m.Rehydrate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string id, string _, string _) => new TestEvent
            {
                Id = id
            });
        var domainFactory = new Mock<IDomainFactory>();
        var store = new Mock<IEventStore>();
        store.Setup(s => s.AddEventsAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<List<EventSourcedChangeEvent>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("astreamname");
        _eventSourcingStore =
            new EventSourcingDddCommandStore<TestEventingAggregateRoot>(recorder.Object, domainFactory.Object,
                migrator.Object, store.Object);
        _messageBroker = new TestMessageBroker();
        _consumerRelay = new TestConsumerRelay();
        var registration =
            new TestNotificationRegistration
            {
                IntegrationEventTranslator = new NoOpIntegrationEventNotificationTranslator<TestEventingAggregateRoot>()
            };
        var registrations = new List<IEventNotificationRegistration>
        {
            registration
        };

        _relay = new InProcessEventNotifyingStoreNotificationRelay(recorder.Object, migrator.Object, _consumerRelay,
            _messageBroker,
            registrations, _eventSourcingStore);
    }

    [Fact]
    public void WhenStart_ThenStarted()
    {
        _relay.Start();

        _relay.IsStarted.Should().BeTrue();
    }

    [Fact]
    public async Task WhenEventHandlerFired_ThenNotifierNotifies()
    {
        var aggregate = new TestEventingAggregateRoot("anid".ToId());
        aggregate.AddEvents(new TestEvent
        {
            Id = "aneventid1"
        }, new TestEvent
        {
            Id = "aneventid2"
        });
        _relay.Start();

        await _eventSourcingStore.SaveAsync(aggregate, CancellationToken.None);

        _consumerRelay.NotifiedEvents.Length.Should().Be(2);
        _consumerRelay.NotifiedEvents[0].As<TestEvent>().Id.Should().Be("aneventid1");
        _consumerRelay.NotifiedEvents[1].As<TestEvent>().Id.Should().Be("aneventid2");

        _messageBroker.ProjectedEvents.Length.Should().Be(0);
    }
}

public class TestNotificationRegistration : IEventNotificationRegistration
{
    public required IIntegrationEventNotificationTranslator IntegrationEventTranslator { get; set; }
}