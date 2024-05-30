using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Infrastructure.Eventing.Common.Notifications;
using Infrastructure.Eventing.Interfaces.Notifications;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Eventing.Common.UnitTests.Notifications;

[Trait("Category", "Unit")]
public class InProcessSynchronousConsumerRelaySpec
{
    private readonly Mock<IDomainEventNotificationConsumer> _domainConsumer;
    private readonly InProcessSynchronousConsumerRelay _relay;

    public InProcessSynchronousConsumerRelaySpec()
    {
        _domainConsumer = new Mock<IDomainEventNotificationConsumer>();
        _domainConsumer.Setup(c => c.NotifyAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        _relay = new InProcessSynchronousConsumerRelay([_domainConsumer.Object]);
    }

    [Fact]
    public async Task WhenRelayAsyncAndNoConsumers_ThenDoesNothing()
    {
        var relay = new InProcessSynchronousConsumerRelay([]);

        var result = await relay.RelayDomainEventAsync(new TestDomainEvent(), new EventStreamChangeEvent
        {
            Data = null!,
            RootAggregateType = "atypename",
            EventType = null!,
            Id = null!,
            LastPersistedAtUtc = default,
            Metadata = null!,
            StreamName = null!,
            Version = 0
        }, CancellationToken.None);

        result.Should().BeSuccess();
        _domainConsumer.Verify(c => c.NotifyAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task WhenRelayAsyncAndConsumerReturnsError_ThenReturnsError()
    {
        _domainConsumer.Setup(c => c.NotifyAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.RuleViolation("amessage"));

        var result = await _relay.RelayDomainEventAsync(new TestDomainEvent(), new EventStreamChangeEvent
        {
            Data = null!,
            RootAggregateType = "atypename",
            EventType = null!,
            Id = null!,
            LastPersistedAtUtc = default,
            Metadata = new EventMetadata("anfqn"),
            StreamName = null!,
            Version = 0
        }, CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected, Error.RuleViolation("amessage")
            .Wrap(Resources.InProcessSynchronousConsumerRelay_ConsumerFailed.Format(
                "IDomainEventNotificationConsumerProxy",
                "arootid", "anfqn")).ToString());
        _domainConsumer.Verify(c => c.NotifyAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "arootid"
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRelayAsyncAndConsumerSucceeds_ThenReturns()
    {
        _domainConsumer.Setup(c => c.NotifyAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Ok);

        var result = await _relay.RelayDomainEventAsync(new TestDomainEvent(), new EventStreamChangeEvent
        {
            Data = null!,
            RootAggregateType = "atypename",
            EventType = null!,
            Id = null!,
            LastPersistedAtUtc = default,
            Metadata = new EventMetadata("anfqn"),
            StreamName = null!,
            Version = 0
        }, CancellationToken.None);

        result.Should().BeSuccess();
        _domainConsumer.Verify(c => c.NotifyAsync(It.Is<TestDomainEvent>(e =>
            e.RootId == "arootid"
        ), It.IsAny<CancellationToken>()));
    }
}