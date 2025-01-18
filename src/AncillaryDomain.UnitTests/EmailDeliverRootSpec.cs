using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.Ancillary.EmailDelivery;
using Domain.Interfaces.Entities;
using Domain.Shared;
using Domain.Shared.Ancillary;
using FluentAssertions;
using Moq;
using UnitTesting.Common;
using UnitTesting.Common.Validation;
using Xunit;

namespace AncillaryDomain.UnitTests;

[Trait("Category", "Unit")]
public class EmailDeliverRootSpec
{
    private readonly Mock<IIdentifierFactory> _idFactory;
    private readonly Mock<IRecorder> _recorder;

    public EmailDeliverRootSpec()
    {
        _recorder = new Mock<IRecorder>();
        _idFactory = new Mock<IIdentifierFactory>();
        _idFactory.Setup(idf => idf.Create(It.IsAny<IIdentifiableEntity>()))
            .Returns("anid".ToId());
    }

    [Fact]
    public void WhenCreate_ThenReturnsAssigned()
    {
        var messageId = CreateMessageId();

        var result = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, "anorganizationid".ToId())
            .Value;

        result.MessageId.Should().Be(messageId);
        result.OrganizationId.Should().Be("anorganizationid".ToId());
        result.Events.Last().Should().BeOfType<Created>();
    }

    [Fact]
    public void WhenSetContentForHtmlEmailAndMissingSubject_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;
        var recipient = EmailRecipient.Create(EmailAddress.Create("auser@company.com").Value, "adisplayname").Value;

        var result = root.SetContent(string.Empty, "abody", recipient, new List<string>());

        result.Should().BeError(ErrorCode.Validation, Resources.EmailDeliveryRoot_HtmlEmail_MissingSubject);
    }

    [Fact]
    public void WhenSetContentForHtmlEmailAndMissingBody_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;
        var recipient = EmailRecipient.Create(EmailAddress.Create("auser@company.com").Value, "adisplayname").Value;

        var result = root.SetContent("asubject", string.Empty, recipient, new List<string>());

        result.Should().BeError(ErrorCode.Validation, Resources.EmailDeliveryRoot_HtmlEmail_MissingBody);
    }

    [Fact]
    public void WhenSetContentForHtmlEmail_ThenDetailsAssigned()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;
        var recipient = EmailRecipient.Create(EmailAddress.Create("auser@company.com").Value, "adisplayname").Value;

        var result = root.SetContent("asubject", "abody", recipient, new List<string> { "atag" });

        result.Should().BeSuccess();
        root.ContentType.Should().Be(DeliveredEmailContentType.Html);
        root.Recipient.Should().Be(recipient);
        root.Tags.Count.Should().Be(1);
        root.Tags[0].Should().Be("atag");
        root.Events.Last().Should().BeOfType<EmailDetailsChanged>();
    }

    [Fact]
    public void WhenSetContentForTemplatedEmailAndMissingTemplateId_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;
        var recipient = EmailRecipient.Create(EmailAddress.Create("auser@company.com").Value, "adisplayname").Value;

        var result = root.SetContent(string.Empty, "asubject", new Dictionary<string, string>(), recipient,
            new List<string>());

        result.Should().BeError(ErrorCode.Validation, Resources.EmailDeliveryRoot_TemplatedEmail_MissingTemplateId);
    }

    [Fact]
    public void WhenSetContentForTemplatedEmail_ThenDetailsAssigned()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;
        var recipient = EmailRecipient.Create(EmailAddress.Create("auser@company.com").Value, "adisplayname").Value;

        var result = root.SetContent("atemplateid", "asubject",
            new Dictionary<string, string> { { "aname", "avalue" } }, recipient,
            new List<string> { "atag" });

        result.Should().BeSuccess();
        root.ContentType.Should().Be(DeliveredEmailContentType.Templated);
        root.Recipient.Should().Be(recipient);
        root.Tags.Count.Should().Be(1);
        root.Tags[0].Should().Be("atag");
        root.Events.Last().Should().BeOfType<EmailDetailsChanged>();
    }

    [Fact]
    public void WhenAttemptSendingAndAlreadyDelivered_ThenDoesNotAttemptAndReturnsTrue()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;
#if TESTINGONLY
        root.TestingOnly_DeliverEmail();
#endif

        var result = root.AttemptSending();

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public void WhenAttemptSendingAndNotDelivered_ThenAddsAnAttempt()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;

        var result = root.AttemptSending();

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
        root.Attempts.Attempts.Count.Should().Be(1);
        root.Attempts.Attempts[0].Should().BeNear(DateTime.UtcNow);
        root.Events.Last().Should().BeOfType<SendingAttempted>();
    }

    [Fact]
    public void WhenFailSendingAndDelivered_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;
#if TESTINGONLY
        root.TestingOnly_DeliverEmail();
#endif

        var result = root.FailedSending();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EmailDeliveryRoot_AlreadySent);
    }

    [Fact]
    public void WhenFailSendingAndNotAttempted_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;

        var result = root.FailedSending();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EmailDeliveryRoot_NotAttempted);
    }

    [Fact]
    public void WhenFailSending_ThenFails()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;
        root.AttemptSending();

        var result = root.FailedSending();

        result.Should().BeSuccess();
        root.Events.Last().Should().BeOfType<SendingFailed>();
    }

    [Fact]
    public void WhenSucceededSendingAndDelivered_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;
#if TESTINGONLY
        root.TestingOnly_DeliverEmail();
#endif

        var result = root.SucceededSending(Optional<string>.None);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EmailDeliveryRoot_AlreadySent);
    }

    [Fact]
    public void WhenSucceededSendingAndNotAttempted_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;

        var result = root.SucceededSending(Optional<string>.None);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EmailDeliveryRoot_NotAttempted);
    }

    [Fact]
    public void WhenSucceededSending_ThenSent()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;
        root.AttemptSending();

        var result = root.SucceededSending("areceiptid");

        result.Should().BeSuccess();
        root.IsSent.Should().BeTrue();
        root.IsDelivered.Should().BeFalse();
        root.Sent.Should().BeNear(DateTime.UtcNow);
        root.Delivered.Should().BeNone();
        root.Events.Last().Should().BeOfType<SendingSucceeded>();
    }

    [Fact]
    public void WhenConfirmDeliveryAndNotSent_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;

        var result = root.ConfirmDelivery("areceiptid", DateTime.UtcNow);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EmailDeliveryRoot_NotSent);
    }

    [Fact]
    public void WhenConfirmDeliveryAndAlreadyDelivered_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;
        root.AttemptSending();
        root.SucceededSending("areceiptid");
        root.ConfirmDelivery("areceiptid", DateTime.UtcNow);

        var result = root.ConfirmDelivery("areceiptid", DateTime.UtcNow);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EmailDeliveryRoot_AlreadyDelivered);
    }

    [Fact]
    public void WhenConfirmDeliveryAnd_ThenConfirmsDelivery()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;
        root.AttemptSending();
        root.SucceededSending("areceiptid");
        var deliveredAt = DateTime.UtcNow;

        var result = root.ConfirmDelivery("areceiptid", deliveredAt);

        result.Should().BeSuccess();
        root.IsDelivered.Should().BeTrue();
        root.Delivered.Should().Be(deliveredAt);
        root.Events.Last().Should().BeOfType<DeliveryConfirmed>();
    }

    [Fact]
    public void WhenConfirmDeliveryFailedAndNotSent_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;

        var result = root.ConfirmDeliveryFailed("areceiptid", DateTime.UtcNow, "areason");

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EmailDeliveryRoot_NotSent);
    }

    [Fact]
    public void WhenConfirmDeliveryFailedAndAlreadyDelivered_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;
        root.AttemptSending();
        root.SucceededSending("areceiptid");
        root.ConfirmDelivery("areceiptid", DateTime.UtcNow);

        var result = root.ConfirmDeliveryFailed("areceiptid", DateTime.UtcNow, "areason");

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EmailDeliveryRoot_AlreadyDelivered);
    }

    [Fact]
    public void WhenConfirmDeliveryFailedAnd_ThenConfirmsDelivery()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId, Optional<Identifier>.None)
            .Value;
        root.AttemptSending();
        root.SucceededSending("areceiptid");
        var failedAt = DateTime.UtcNow;

        var result = root.ConfirmDeliveryFailed("areceiptid", failedAt, "areason");

        result.Should().BeSuccess();
        root.IsDelivered.Should().BeFalse();
        root.Delivered.Should().BeNone();
        root.IsFailedDelivery.Should().BeTrue();
        root.FailedDelivery.Should().Be(failedAt);
        root.Events.Last().Should().BeOfType<DeliveryFailureConfirmed>();
    }

    private static QueuedMessageId CreateMessageId()
    {
        var messageId = new MessageQueueMessageIdFactory().Create("aqueuename");
        return QueuedMessageId.Create(messageId).Value;
    }
}