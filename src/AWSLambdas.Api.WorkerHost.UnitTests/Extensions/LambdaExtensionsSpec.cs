using Amazon.Lambda.SQSEvents;
using Application.Persistence.Interfaces;
using AWSLambdas.Api.WorkerHost.Extensions;
using FluentAssertions;
using Infrastructure.Workers.Api;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace AWSLambdas.Api.WorkerHost.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class LambdaExtensionsSpec
{
    private readonly Mock<IMessageDeliveryHandler> _handler;
    private readonly SQSEvent _receivedMessage;
    private readonly Mock<IMessageBusMonitoringApiRelayWorker<TestMessage>> _worker;

    public LambdaExtensionsSpec()
    {
        _receivedMessage = new SQSEvent
        {
            Records =
            [
                new SQSEvent.SQSMessage
                {
                    Body = "{}"
                }
            ]
        };
        _worker = new Mock<IMessageBusMonitoringApiRelayWorker<TestMessage>>();
        _handler = new Mock<IMessageDeliveryHandler>();
        _handler.Setup(hdl => hdl.FunctionName)
            .Returns("afunctionname");
        _handler.Setup(hdl => hdl.RetryCount)
            .Returns(5);
    }

    [Fact]
    public async Task WhenHandleDeliveryAndRelaySucceeds_ThenCompletesMessage()
    {
        _worker.Setup(wkr => wkr.RelayMessageOrThrowAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await _handler.Object.HandleDelivery(_receivedMessage, _worker.Object,
            "asubscriberhostname", "asubscriptionname", CancellationToken.None);

        _worker.Verify(wkr => wkr.RelayMessageOrThrowAsync("asubscriberhostname", "asubscriptionname",
            It.IsAny<TestMessage>(), CancellationToken.None));
        _handler.Verify(hdl => hdl.CompleteMessageAsync(_receivedMessage, CancellationToken.None));
        _handler.Verify(
            hdl => hdl.CheckCircuitAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>(),
                It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WhenHandleDeliveryAndRelayFailsAndFirstAttempt_ThenAbandonsAndChecksCircuitAndThrows()
    {
        _worker.Setup(wkr => wkr.RelayMessageOrThrowAsync(It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<TestMessage>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("amessage"));

        await _handler.Object.Invoking(x => x.HandleDelivery(_receivedMessage, _worker.Object,
                "asubscriberhostname", "asubscriptionname", CancellationToken.None))
            .Should().ThrowAsync<Exception>()
            .WithMessage("amessage");

        _worker.Verify(wkr => wkr.RelayMessageOrThrowAsync("asubscriberhostname", "asubscriptionname",
            It.IsAny<TestMessage>(), CancellationToken.None));
        _handler.Verify(hdl => hdl.CheckCircuitAsync("afunctionname", 0, 5, CancellationToken.None));
    }
}

[UsedImplicitly]
public class TestMessage : IQueuedMessage
{
    public string CallerId { get; set; } = "acallerid";

    public string CallId { get; set; } = "acallid";

    public string? MessageId { get; set; }

    public string? OriginHostRegion { get; set; }

    public string? TenantId { get; set; }
}