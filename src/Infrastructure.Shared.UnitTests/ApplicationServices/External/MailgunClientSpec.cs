using Application.Interfaces;
using Common;
using FluentAssertions;
using Infrastructure.Shared.ApplicationServices.External;
using Infrastructure.Web.Api.Operations.Shared._3rdParties.Mailgun;
using Infrastructure.Web.Interfaces.Clients;
using Moq;
using Polly;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Shared.UnitTests.ApplicationServices.External;

[Trait("Category", "Unit")]
public class MailgunClientSpec
{
    private readonly Mock<ICallContext> _call;
    private readonly MailgunClient _client;
    private readonly Mock<IServiceClient> _serviceClient;

    public MailgunClientSpec()
    {
        _call = new Mock<ICallContext>();
        var recorder = new Mock<IRecorder>();
        _serviceClient = new Mock<IServiceClient>();
        var retryPolicy = Policy.NoOpAsync().As<IAsyncPolicy>();

        _client = new MailgunClient(recorder.Object, _serviceClient.Object, retryPolicy, "anapikey", "adomainname");
    }

    [Fact]
    public async Task WhenSendAsync_ThenSends()
    {
        _serviceClient.Setup(sc => sc.PostAsync(It.IsAny<ICallerContext>(), It.IsAny<MailgunSendMessageRequest>(),
                It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MailgunSendMessageResponse
            {
                Id = "areceiptid",
                Message = "amessage"
            });

        var result = await _client.SendHtmlAsync(_call.Object, "asubject", "afromemailaddress", "afromdisplayname",
            new MailGunRecipient { DisplayName = "atodisplayname", EmailAddress = "atoemailaddress" }, "anhtmlmessage",
            ["atag", "anothertag"],
            CancellationToken.None);

        result.Should().BeSuccess();
        result.Value.ReceiptId.Should().Be("areceiptid");
        _serviceClient.Verify(sc => sc.PostAsync(It.IsAny<ICallerContext>(), It.Is<MailgunSendMessageRequest>(req =>
                req.DomainName == "adomainname"
                && req.To == "atoemailaddress"
                && req.From == "afromdisplayname <afromemailaddress>"
                && req.Subject == "asubject"
                && req.RecipientVariables
                == "{\r\n  \"atoemailaddress\": {\r\n    \"Name\": \"atodisplayname\",\r\n    \"Id\": 1\r\n  }\r\n}"
                && req.Tags![0] == "atag"
                && req.Tags![1] == "anothertag"
                && req.TestingOnly == "yes"
                && req.Tracking == "no"
            ),
            It.IsAny<Action<HttpRequestMessage>>(), It.IsAny<CancellationToken>()));
    }
}