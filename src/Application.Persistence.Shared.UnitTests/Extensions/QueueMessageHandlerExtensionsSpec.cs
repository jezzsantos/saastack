using Application.Persistence.Interfaces;
using Application.Persistence.Shared.Extensions;
using Application.Persistence.Shared.ReadModels;
using Common;
using Common.Extensions;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using Xunit;

namespace Application.Persistence.Shared.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class QueueMessageHandlerExtensionsSpec
{
    [Fact]
    public void WhenRehydrateQueuedMessageAndMessageIsNotRehydratable_ThenReturnsError()
    {
        var result = "notvalidjson".RehydrateQueuedMessage<EmailMessage>();

        result.Should().BeError(ErrorCode.RuleViolation,
            Resources.QueueMessageHandlerExtensions_InvalidQueuedMessage.Format(nameof(EmailMessage),
                "notvalidjson"));
    }

    [Fact]
    public void WhenRehydrateQueuedMessageAndRehydratable_ThenReturnsMessage()
    {
        var result = new EmailMessage
            {
                MessageId = "amessageid",
                Html = new QueuedEmailHtmlMessage
                {
                    Subject = "asubject",
                    Body = "abody"
                }
            }.ToJson()!
            .RehydrateQueuedMessage<EmailMessage>();

        result.Should().BeSuccess();
        result.Value.MessageId.Should().Be("amessageid");
        result.Value.Html!.Subject.Should().Be("asubject");
        result.Value.Html.Body.Should().Be("abody");
    }

    [Fact]
    public async Task WhenDrainAllQueuedMessagesAsyncAndNoMessage_ThenNotCallHandler()
    {
        var repository = new Mock<IMessageQueueStore<EmailMessage>>();
        var message = new EmailMessage
        {
            MessageId = "amessageid",
            Html = new QueuedEmailHtmlMessage
            {
                Subject = "asubject",
                Body = "abody"
            }
        };
        var handler = new Mock<Func<EmailMessage, Task<Result<bool, Error>>>>();
        handler.Setup(h => h(message))
            .ReturnsAsync(Error.Unexpected("anerror"));
        repository.Setup(rep => rep.PopSingleAsync(
                It.IsAny<Func<EmailMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        await repository.Object.DrainAllQueuedMessagesAsync(handler.Object, CancellationToken.None);

        handler.Verify(x => x(message), Times.Never);
    }

    [Fact]
    public async Task WhenDrainAllQueuedMessagesAsyncAndOneMessage_ThenOnlyPopsOnce()
    {
        var repository = new Mock<IMessageQueueStore<EmailMessage>>();
        repository.Setup(x => x.PopSingleAsync(It.IsAny<Func<EmailMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var handler = new Mock<Func<EmailMessage, Task<Result<bool, Error>>>>();

        await repository.Object.DrainAllQueuedMessagesAsync(handler.Object, CancellationToken.None);

        repository.Verify(rep => rep.PopSingleAsync(
            It.IsAny<Func<EmailMessage, CancellationToken, Task<Result<Error>>>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task WhenDrainAllQueuedMessagesAsyncAndManyMessages_ThenPopsMany()
    {
        var repository = new Mock<IMessageQueueStore<EmailMessage>>();
        repository.SetupSequence(x => x.PopSingleAsync(
                It.IsAny<Func<EmailMessage, CancellationToken, Task<Result<Error>>>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true)
            .ReturnsAsync(true)
            .ReturnsAsync(false);
        var handler = new Mock<Func<EmailMessage, Task<Result<bool, Error>>>>();

        await repository.Object.DrainAllQueuedMessagesAsync(handler.Object, CancellationToken.None);

        repository.Verify(rep => rep.PopSingleAsync(
            It.IsAny<Func<EmailMessage, CancellationToken, Task<Result<Error>>>>(),
            It.IsAny<CancellationToken>()), Times.Exactly(3));
    }
}