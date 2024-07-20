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
public class MailgunHttpServiceClientSpec
{
    private readonly Mock<ICallerContext> _caller;
    private readonly Mock<IMailgunClient> _client;
    private readonly MailgunHttpServiceClient _serviceClient;

    public MailgunHttpServiceClientSpec()
    {
        _caller = new Mock<ICallerContext>();
        var recorder = new Mock<IRecorder>();
        _client = new Mock<IMailgunClient>();

        _serviceClient = new MailgunHttpServiceClient(recorder.Object, _client.Object);
    }

    [Fact]
    public async Task WhenDeliverAsync_ThenSends()
    {
        _client.Setup(c => c.SendAsync(It.IsAny<ICallContext>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<MailGunRecipient>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmailDeliveryReceipt
            {
                ReceiptId = "areceiptid"
            });

        var result = await _serviceClient.SendAsync(_caller.Object, "asubject", "anhtmlbody", "atoemailaddress",
            "atodisplayname", "afromemailaddress", "afromdisplayname");

        result.Should().BeSuccess();
        result.Value.ReceiptId.Should().Be("areceiptid");
        _client.Verify(c => c.SendAsync(It.IsAny<ICallContext>(), "asubject", "afromemailaddress", "afromdisplayname",
            It.Is<MailGunRecipient>(r => r.EmailAddress == "atoemailaddress" && r.DisplayName == "atodisplayname"),
            "anhtmlbody", It.IsAny<CancellationToken>()));
    }
}