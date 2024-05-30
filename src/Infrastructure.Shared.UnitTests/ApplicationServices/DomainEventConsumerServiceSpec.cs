using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Interfaces.Notifications;
using Infrastructure.Shared.ApplicationServices;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Shared.UnitTests.ApplicationServices;

[Trait("Category", "Unit")]
public class DomainEventConsumerServiceSpec
{
    private readonly Mock<IDomainEventNotificationConsumer> _consumer;
    private readonly Mock<IEventSourcedChangeEventMigrator> _migrator;
    private readonly DomainEventConsumerService _service;

    public DomainEventConsumerServiceSpec()
    {
        _migrator = new Mock<IEventSourcedChangeEventMigrator>();
        _consumer = new Mock<IDomainEventNotificationConsumer>();

        _service = new DomainEventConsumerService([_consumer.Object], _migrator.Object);
    }

    [Fact]
    public async Task WhenNotifyAsyncAndNoConsumers_ThenDoesNothing()
    {
        _migrator.Setup(m => m.Rehydrate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(new TestDomainEvent());
        var service = new DomainEventConsumerService([], _migrator.Object);
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

        var result = await service.NotifyAsync(changeEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _migrator.Verify(m => m.Rehydrate("anid", "{}", "anfqn"));
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

        var result = await _service.NotifyAsync(changeEvent, CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected, Error.RuleViolation("amessage")
            .Wrap(Resources.DomainEventConsumerService_ConsumerFailed.Format(
                "IDomainEventNotificationConsumerProxy",
                "arootid", "anfqn")).ToString());
        _migrator.Verify(m => m.Rehydrate("anid", "{}", "anfqn"));
    }

    [Fact]
    public async Task WhenNotifyAsync_ThenReturns()
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

        var result = await _service.NotifyAsync(changeEvent, CancellationToken.None);

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