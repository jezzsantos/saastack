using Common;
using Common.Extensions;
using Domain.Common;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Shared;
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

        var result = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId).Value;

        result.MessageId.Should().Be(messageId);
        result.Events.Last().Should().BeOfType<Events.EmailDelivery.Created>();
    }

    [Fact]
    public void WhenSetEmailDetailsAndMissingSubject_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId).Value;
        var recipient = EmailRecipient.Create(EmailAddress.Create("auser@company.com").Value, "adisplayname").Value;

        var result = root.SetEmailDetails(string.Empty, "abody", recipient);

        result.Should().BeError(ErrorCode.Validation, Resources.EmailDeliveryRoot_MissingEmailSubject);
    }

    [Fact]
    public void WhenSetEmailDetailsAndMissingBody_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId).Value;
        var recipient = EmailRecipient.Create(EmailAddress.Create("auser@company.com").Value, "adisplayname").Value;

        var result = root.SetEmailDetails("asubject", string.Empty, recipient);

        result.Should().BeError(ErrorCode.Validation, Resources.EmailDeliveryRoot_MissingEmailBody);
    }

    [Fact]
    public void WhenSetEmailDetails_ThenDetailsAssigned()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId).Value;
        var recipient = EmailRecipient.Create(EmailAddress.Create("auser@company.com").Value, "adisplayname").Value;

        var result = root.SetEmailDetails("asubject", "abody", recipient);

        result.Should().BeSuccess();
        root.Recipient.Should().Be(recipient);
        root.Events.Last().Should().BeOfType<Events.EmailDelivery.EmailDetailsChanged>();
    }

    [Fact]
    public void WhenAttemptedDeliveryAndAlreadyDelivered_ThenDoesNotAttemptAndReturnsTrue()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId).Value;
#if TESTINGONLY
        root.TestingOnly_DeliverEmail();
#endif

        var result = root.AttemptDelivery();

        result.Should().BeSuccess();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public void WhenAttemptedDeliveryAndNotDelivered_ThenAddsAnAttempt()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId).Value;

        var result = root.AttemptDelivery();

        result.Should().BeSuccess();
        result.Value.Should().BeFalse();
        root.Attempts.Attempts.Count.Should().Be(1);
        root.Attempts.Attempts[0].Should().BeNear(DateTime.UtcNow);
        root.Events.Last().Should().BeOfType<Events.EmailDelivery.DeliveryAttempted>();
    }

    [Fact]
    public void WhenFailDeliveryAndDelivered_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId).Value;
#if TESTINGONLY
        root.TestingOnly_DeliverEmail();
#endif

        var result = root.FailedDelivery();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EmailDeliveryRoot_AlreadyDelivered);
    }

    [Fact]
    public void WhenFailDeliveryAndNotAttempted_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId).Value;

        var result = root.FailedDelivery();

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EmailDeliveryRoot_NotAttempted);
    }

    [Fact]
    public void WhenFailDelivery_ThenFails()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId).Value;
        root.AttemptDelivery();

        var result = root.FailedDelivery();

        result.Should().BeSuccess();
        root.Events.Last().Should().BeOfType<Events.EmailDelivery.DeliveryFailed>();
    }

    [Fact]
    public void WhenSucceededDeliveryAndDelivered_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId).Value;
#if TESTINGONLY
        root.TestingOnly_DeliverEmail();
#endif

        var result = root.SucceededDelivery(Optional<string>.None);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EmailDeliveryRoot_AlreadyDelivered);
    }

    [Fact]
    public void WhenSucceededDeliveryAndNotAttempted_ThenReturnsError()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId).Value;

        var result = root.SucceededDelivery(Optional<string>.None);

        result.Should().BeError(ErrorCode.RuleViolation, Resources.EmailDeliveryRoot_NotAttempted);
    }

    [Fact]
    public void WhenSucceededDelivery_ThenDelivered()
    {
        var messageId = CreateMessageId();
        var root = EmailDeliveryRoot.Create(_recorder.Object, _idFactory.Object, messageId).Value;
        root.AttemptDelivery();

        var result = root.SucceededDelivery("atransactionid");

        result.Should().BeSuccess();
        root.IsDelivered.Should().BeTrue();
        root.Delivered.Should().BeNear(DateTime.UtcNow);
        root.Events.Last().Should().BeOfType<Events.EmailDelivery.DeliverySucceeded>();
    }
    
    private static QueuedMessageId CreateMessageId()
    {
        var messageId = new MessageQueueIdFactory().Create("aqueuename");
        return QueuedMessageId.Create(messageId).Value;
    }
}