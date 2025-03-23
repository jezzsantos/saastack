using Application.Persistence.Interfaces;
using Common;
using Common.Configuration;
using Common.Extensions;
using Common.Recording;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using EventNotificationsInfrastructure.ApplicationServices;
using FluentAssertions;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Persistence.Interfaces;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace EventNotificationsInfrastructure.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class DomainEventingSubscriberSpec
{
    private readonly Mock<IDomainEventNotificationConsumer> _consumer;
    private readonly Mock<IEventSourcedChangeEventMigrator> _migrator;
    private readonly Mock<IRecorder> _recorder;
    private readonly DomainEventingSubscriber _subscriber;

    public DomainEventingSubscriberSpec()
    {
        _recorder = new Mock<IRecorder>();
        _migrator = new Mock<IEventSourcedChangeEventMigrator>();
        _consumer = new Mock<IDomainEventNotificationConsumer>();
        var settings = new Mock<IConfigurationSettings>();
        settings.Setup(s =>
                s.Platform.GetString(DomainEventingSubscriber.SubscriptionNameSettingName, It.IsAny<string>()))
            .Returns("ahostname");

        _subscriber =
            new DomainEventingSubscriber(_recorder.Object, settings.Object, _migrator.Object, _consumer.Object);
    }

    [Fact]
    public void WhenGetSubscriptionName_ThenReturns()
    {
        var result = _subscriber.SubscriptionName;

        result.Should().Be("avalue");
    }

    [Fact]
    public async Task WhenSubscribe_ThenSubscribesToStore()
    {
        var store = new Mock<IMessageBusStore>();

        var result = await _subscriber.SubscribeAsync(store.Object, "atopicname", CancellationToken.None);

        result.Should().BeSuccess();
        store.Verify(s => s.SubscribeAsync("atopicname", "avalue", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenNotifyAsyncAndConsumerReturnsError_ThenReturnsError()
    {
        _migrator.Setup(m => m.Rehydrate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new TestDomainEvent());
        _consumer.Setup(c => c.NotifyAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.RuleViolation("amessage"));
        var changeEvent = new EventStreamChangeEvent
        {
            Data = "{}",
            RootAggregateType = "atype",
            EventType = "aneventtype",
            Id = "anid",
            LastPersistedAtUtc = default,
            Metadata = new EventMetadata("anfqn"),
            StreamName = "astreamname",
            Version = 1
        };

        var result = await _subscriber.NotifyAsync(changeEvent, CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected, Error.RuleViolation("amessage")
            .Wrap(Resources.DomainEventingSubscriber_ConsumerFailed.Format(
                "IDomainEventNotificationConsumerProxy",
                "arootid", "anfqn")).ToString());
        _migrator.Verify(m => m.Rehydrate("anid", "{}", "anfqn"));
        _recorder.Verify(rec =>
            rec.Crash(null, CrashLevel.Critical, It.Is<Exception>(ex =>
                ex.Message == "amessage"
            ), It.IsAny<string>(), It.IsAny<object[]>()));
    }

    [Fact]
    public async Task WhenNotifyAsync_ThenNotifies()
    {
        var domainEvent = new TestDomainEvent
        {
            RootId = "arootid",
            AProperty = "avalue",
            OccurredUtc = DateTime.UtcNow
        };
        _migrator.Setup(m => m.Rehydrate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(domainEvent);
        _consumer.Setup(c => c.NotifyAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
        var eventJson = domainEvent.ToJson()!;
        var changeEvent = new EventStreamChangeEvent
        {
            Data = eventJson,
            RootAggregateType = "atype",
            EventType = "aneventtype",
            Id = "anid",
            LastPersistedAtUtc = default,
            Metadata = new EventMetadata("anfqn"),
            StreamName = "astreamname",
            Version = 1
        };

        var result = await _subscriber.NotifyAsync(changeEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _migrator.Verify(m => m.Rehydrate("anid", eventJson, "anfqn"));
        _consumer.Verify(c => c.NotifyAsync(It.Is<TestDomainEvent>(evt =>
            evt.RootId == "arootid"
            && evt.OccurredUtc.HasValue()
            && evt.AProperty == "avalue"
        ), It.IsAny<CancellationToken>()));
    }
}

public class TestDomainEvent : IDomainEvent
{
    public string? AProperty { get; set; }

    public DateTime OccurredUtc { get; set; }

    public string RootId { get; set; } = "arootid";
}