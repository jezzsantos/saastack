using Application.Persistence.Interfaces;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using EventNotificationsInfrastructure.ApplicationServices;
using FluentAssertions;
using Infrastructure.Eventing.Interfaces.Notifications;
using Moq;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace EventNotificationsInfrastructure.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class ApiHostDomainEventingConsumerServiceSpec
{
    private readonly Mock<IEventSourcedChangeEventMigrator> _migrator;
    private readonly Mock<IDomainEventNotificationConsumer> _notificationConsumer;
    private readonly Mock<IRecorder> _recorder;
    private readonly ApiHostDomainEventingConsumerService _service;
    private readonly Mock<IDomainEventingSubscriberService> _subscriberService;
    private readonly Mock<ApiHostDomainEventingConsumerService.IDomainEventingSubscribingConsumer> _subscribingConsumer;

    public ApiHostDomainEventingConsumerServiceSpec()
    {
        _recorder = new Mock<IRecorder>();
        _notificationConsumer = new Mock<IDomainEventNotificationConsumer>();
        _subscribingConsumer = new Mock<ApiHostDomainEventingConsumerService.IDomainEventingSubscribingConsumer>();
        var consumers = new List<ApiHostDomainEventingConsumerService.IDomainEventingSubscribingConsumer>
            { _subscribingConsumer.Object };
        _subscriberService = new Mock<IDomainEventingSubscriberService>();
        _subscriberService.SetupGet(ss => ss.Consumers)
            .Returns(new Dictionary<Type, string>
            {
                { _notificationConsumer.Object.GetType(), "asubscriptionname1" }
            });
        _migrator = new Mock<IEventSourcedChangeEventMigrator>();

        _service =
            new ApiHostDomainEventingConsumerService(_subscriberService.Object, consumers);
    }

    [Fact]
    public void WhenConstructedAndInjectedConsumerButNotRegistered_ThenThrows()
    {
        _subscriberService.SetupGet(ss => ss.Consumers)
            .Returns(new Dictionary<Type, string>());
        var consumers = new List<IDomainEventNotificationConsumer>
            { _notificationConsumer.Object };

        FluentActions.Invoking(() =>
                new ApiHostDomainEventingConsumerService(_recorder.Object, consumers, _migrator.Object,
                    _subscriberService.Object))
            .Should().Throw<InvalidOperationException>()
            .WithMessageLike(
                Resources.ApiHostDomainEventingConsumerService_WrapConsumers_MissingFromRegistration.Format(
                    "Castle.Proxies.IDomainEventNotificationConsumerProxy"));
    }

    [Fact]
    public void WhenConstructedAndRegisteredConsumerButNotInjected_ThenThrows()
    {
        _subscriberService.SetupGet(ss => ss.Consumers)
            .Returns(new Dictionary<Type, string>
            {
                { _notificationConsumer.Object.GetType(), "asubscriptionname1" }
            });

        FluentActions.Invoking(() =>
                new ApiHostDomainEventingConsumerService(_recorder.Object, new List<IDomainEventNotificationConsumer>(),
                    _migrator.Object,
                    _subscriberService.Object))
            .Should().Throw<InvalidOperationException>()
            .WithMessageLike(
                Resources.ApiHostDomainEventingConsumerService_WrapConsumers_MissingFromInjection.Format(
                    "Castle.Proxies.IDomainEventNotificationConsumerProxy"));
    }

    [Fact]
    public void WhenConstructed_ThenHasSubscriptionNames()
    {
        _subscriberService.SetupGet(ss => ss.SubscriptionNames)
            .Returns(["asubscriptionname1"]);

        var result = _service.SubscriptionNames;

        result.Count.Should().Be(1);
        result[0].Should().Be("asubscriptionname1");
        _subscriberService.Verify(ss => ss.SubscriptionNames);
    }

    [Fact]
    public async Task WhenNotifyAsyncAndConsumerNotFound_ThenThrows()
    {
        var domainEvent = new TestDomainEvent
        {
            RootId = "arootid",
            AProperty = "avalue",
            OccurredUtc = DateTime.UtcNow
        };
        _subscribingConsumer.Setup(sc => sc.SubscriptionName)
            .Returns("asubscriptionname");
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

        await _service.Invoking(x =>
                x.NotifySubscriberAsync("anothersubscriptionname", changeEvent, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessageLike(
                Resources.ApiHostDomainEventingConsumerService_NotifySubscriberAsync_MissingConsumer.Format(
                    "anothersubscriptionname"));
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
        _subscribingConsumer.Setup(sc => sc.SubscriptionName)
            .Returns("asubscriptionname");
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

        var result = await _service.NotifySubscriberAsync("asubscriptionname", changeEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _subscribingConsumer.Verify(sc => sc.NotifyAsync(It.Is<EventStreamChangeEvent>(evt =>
            evt.RootAggregateType == "atype"
            && evt.StreamName == "astreamname"
        ), It.IsAny<CancellationToken>()));
    }
}

public class TestDomainEvent : IDomainEvent
{
    public string? AProperty { get; set; }

    public DateTime OccurredUtc { get; set; }

    public string RootId { get; set; } = "arootid";
}