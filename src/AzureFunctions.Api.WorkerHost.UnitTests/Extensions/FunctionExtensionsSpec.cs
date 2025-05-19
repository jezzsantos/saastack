using Application.Persistence.Interfaces;
using Azure.Messaging.ServiceBus;
using AzureFunctions.Api.WorkerHost.Extensions;
using FluentAssertions;
using Infrastructure.Workers.Api;
using JetBrains.Annotations;
using Moq;
using Xunit;

namespace AzureFunctions.Api.WorkerHost.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class FunctionExtensionsSpec
{
    private readonly Mock<IMessageDeliveryHandler> _handler;
    private readonly ServiceBusReceivedMessage _receivedMessage;
    private readonly Mock<IMessageBusMonitoringApiRelayWorker<TestMessage>> _worker;

    public FunctionExtensionsSpec()
    {
        _receivedMessage = ServiceBusModelFactory.ServiceBusReceivedMessage(
            BinaryData.FromString("{}"),
            deliveryCount: 0,
            properties: new Dictionary<string, object>
            {
                { FunctionExtensions.ServiceBusReceivedMessageDeliveryCountPropertyName, 5 }
            });
        _worker = new Mock<IMessageBusMonitoringApiRelayWorker<TestMessage>>();
        _handler = new Mock<IMessageDeliveryHandler>();
        _handler.Setup(hdl => hdl.FunctionName)
            .Returns("afunctionname");
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
            hdl => hdl.CheckCircuitAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
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
        _handler.Verify(hdl => hdl.CheckCircuitAsync("afunctionname", 0, CancellationToken.None));
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