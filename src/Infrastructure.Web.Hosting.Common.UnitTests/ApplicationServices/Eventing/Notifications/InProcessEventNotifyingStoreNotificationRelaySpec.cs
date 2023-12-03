using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using FluentAssertions;
using Infrastructure.Eventing.Common.Notifications;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Web.Hosting.Common.ApplicationServices.Eventing.Notifications;
using Moq;
using Xunit;

namespace Infrastructure.Web.Hosting.Common.UnitTests.ApplicationServices.Eventing.Notifications;

[Trait("Category", "Unit")]
public class InProcessEventNotifyingStoreNotificationRelaySpec
{
    private readonly TestConsumer _consumer;
    private readonly EventSourcingDddCommandStore<TestEventingAggregateRoot> _eventSourcingStore;
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
            .Returns(Task.FromResult<Result<string, Error>>("astreamname"));
        _eventSourcingStore =
            new EventSourcingDddCommandStore<TestEventingAggregateRoot>(recorder.Object, domainFactory.Object,
                migrator.Object, store.Object);
        _consumer = new TestConsumer();
        var registration = new EventNotificationRegistration(
            new PassThroughEventNotificationProducer<TestEventingAggregateRoot>(),
            _consumer);
        var registrations = new List<IEventNotificationRegistration>
        {
            registration
        };

        _relay = new InProcessEventNotifyingStoreNotificationRelay(recorder.Object, migrator.Object, registrations,
            _eventSourcingStore);
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

        _consumer.ProjectedEvents.Length.Should().Be(2);
        _consumer.ProjectedEvents[0].As<TestEvent>().Id.Should().Be("aneventid1");
        _consumer.ProjectedEvents[1].As<TestEvent>().Id.Should().Be("aneventid2");
    }
}