using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Common.Recording;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using EventNotificationsInfrastructure.ApplicationServices;
using FluentAssertions;
using Infrastructure.Eventing.Interfaces.Notifications;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace EventNotificationsInfrastructure.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class ApiHostDomainEventingSubscribingConsumerSpec
{
    private readonly ApiHostDomainEventingConsumerService.ApiHostDomainEventingSubscribingConsumer _consumer;
    private readonly Mock<IEventSourcedChangeEventMigrator> _migrator;
    private readonly Mock<IDomainEventNotificationConsumer> _notificationConsumer;
    private readonly Mock<IRecorder> _recorder;

    public ApiHostDomainEventingSubscribingConsumerSpec()
    {
        _recorder = new Mock<IRecorder>();
        _migrator = new Mock<IEventSourcedChangeEventMigrator>();
        _notificationConsumer = new Mock<IDomainEventNotificationConsumer>();

        _consumer = new ApiHostDomainEventingConsumerService.ApiHostDomainEventingSubscribingConsumer(_recorder.Object,
            "asubscriptionname", _migrator.Object, _notificationConsumer.Object);
    }

    [Fact]
    public void WhenConstructed_ThenSubscriptionName()
    {
        var result = _consumer.SubscriptionName;

        result.Should().Be("asubscriptionname");
    }

    [Fact]
    public async Task WhenNotifyAsyncAndConsumerReturnsError_ThenReturnsError()
    {
        _migrator.Setup(m => m.Rehydrate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new TestDomainEvent());
        _notificationConsumer.Setup(c => c.NotifyAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
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

        var result = await _consumer.NotifyAsync(changeEvent, CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected, Error.RuleViolation("amessage")
            .Wrap(Resources.DomainEventingSubscriber_ConsumerFailed.Format(
                "IDomainEventNotificationConsumerProxy",
                "arootid", "anfqn")).ToString());
        _migrator.Verify(m => m.Rehydrate("anid", "{}", "anfqn"));
        _recorder.Verify(rec =>
            rec.Crash(null, CrashLevel.Critical, It.Is<Exception>(ex =>
                ex.Message.Contains("amessage")
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
        _notificationConsumer.Setup(c => c.NotifyAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);
        var eventJson = domainEvent.ToJson()!;
        var changeEvent = new EventStreamChangeEvent
        {
            Data = eventJson,
            RootAggregateType = "atype",
            EventType = "aneventtype",
            Id = "anid",
            LastPersistedAtUtc = null,
            Metadata = new EventMetadata("anfqn"),
            StreamName = "astreamname",
            Version = 1
        };

        var result = await _consumer.NotifyAsync(changeEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _migrator.Verify(m => m.Rehydrate("anid", eventJson, "anfqn"));
        _notificationConsumer.Verify(c => c.NotifyAsync(It.Is<TestDomainEvent>(evt =>
            evt.RootId == "arootid"
            && evt.OccurredUtc.HasValue()
            && evt.AProperty == "avalue"
        ), It.IsAny<CancellationToken>()));
    }
}