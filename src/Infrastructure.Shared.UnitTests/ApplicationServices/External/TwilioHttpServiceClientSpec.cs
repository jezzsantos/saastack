using Application.Interfaces;
using Application.Persistence.Shared;
using Common;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices.External;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Shared.UnitTests.ApplicationServices.External;

[Trait("Category", "Unit")]
public class TwilioHttpServiceClientSpec
{
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<ITwilioClient> _client;
    private readonly TwilioHttpServiceClient _serviceClient;

    public TwilioHttpServiceClientSpec()
    {
        _caller = new Mock<ICallerContext>();
        var recorder = new Mock<IRecorder>();
        _client = new Mock<ITwilioClient>();

        _serviceClient = new TwilioHttpServiceClient(recorder.Object, _client.Object);
    }

    [Fact]
    public async Task WhenDeliverAsync_ThenSends()
    {
        _client.Setup(c => c.SendAsync(It.IsAny<ICallContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<IReadOnlyList<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmsDeliveryReceipt
            {
                ReceiptId = "areceiptid"
            });

        var result = await _serviceClient.SendAsync(_caller.Object, "abody", "aphonenumber", ["atag", "anothertag"],
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.ReceiptId.Should().Be("areceiptid");
        _client.Verify(c =>
            c.SendAsync(It.IsAny<ICallContext>(), "aphonenumber", "abody", It.Is<IReadOnlyList<string>>(tags =>
                tags.Count == 2
                && tags[0] == "atag"
                && tags[1] == "anothertag"), It.IsAny<CancellationToken>()));
    }
}