using Application.Persistence.Interfaces;
using Application.Persistence.Shared;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Infrastructure.Eventing.Common.Notifications;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Eventing.Common.UnitTests.Notifications;

[Trait("Category", "Unit")]
public class AsynchronousQueueConsumerRelaySpec
{
    private readonly Mock<IDomainEventingMessageBusTopic> _queue;
    private readonly AsynchronousQueueConsumerRelay _relay;

    public AsynchronousQueueConsumerRelaySpec()
    {
        _queue = new Mock<IDomainEventingMessageBusTopic>();

        _relay = new AsynchronousQueueConsumerRelay(_queue.Object);
    }

    [Fact]
    public async Task WhenRelayAsyncAndQueueReturnsError_ThenReturnsError()
    {
        _queue.Setup(c =>
                c.SendAsync(It.IsAny<ICallContext>(), It.IsAny<DomainEventingMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.RuleViolation("amessage"));
        var changeEvent = new EventStreamChangeEvent
        {
            Data = null!,
            RootAggregateType = "atypename",
            EventType = null!,
            Id = null!,
            LastPersistedAtUtc = default,
            Metadata = new EventMetadata("anfqn"),
            StreamName = null!,
            Version = 0
        };

        var result =
            await _relay.RelayDomainEventAsync(new TestDomainEvent(), changeEvent, CancellationToken.None);

        result.Should().BeError(ErrorCode.Unexpected, Error.RuleViolation("amessage")
            .Wrap(Resources.AsynchronousConsumerRelay_RelayFailed.Format(
                "AsynchronousQueueConsumerRelay",
                "arootid", "anfqn")).ToString());
        _queue.Verify(c => c.SendAsync(It.IsAny<ICallContext>(), It.Is<DomainEventingMessage>(msg =>
            msg.Event == changeEvent
        ), It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task WhenRelayAsyncAndConsumerSucceeds_ThenReturns()
    {
        _queue.Setup(c =>
                c.SendAsync(It.IsAny<ICallContext>(), It.IsAny<DomainEventingMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Result<DomainEventingMessage, Error>());

        var changeEvent = new EventStreamChangeEvent
        {
            Data = null!,
            RootAggregateType = "atypename",
            EventType = null!,
            Id = null!,
            LastPersistedAtUtc = default,
            Metadata = new EventMetadata("anfqn"),
            StreamName = null!,
            Version = 0
        };

        var result =
            await _relay.RelayDomainEventAsync(new TestDomainEvent(), changeEvent, CancellationToken.None);

        result.Should().BeSuccess();
        _queue.Verify(c => c.SendAsync(It.IsAny<ICallContext>(), It.Is<DomainEventingMessage>(msg =>
            msg.Event == changeEvent
        ), It.IsAny<CancellationToken>()));
    }
}