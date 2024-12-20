using Common;
using Domain.Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace AncillaryDomain.UnitTests;

[Trait("Category", "Unit")]
public class QueuedMessageIdSpec
{
    [Fact]
    public void WhenCreatedWithEmptyValue_ThenReturnsError()
    {
        var result = QueuedMessageId.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreatedWithInvalidId_ThenReturnsError()
    {
        var result = QueuedMessageId.Create("aninvalidid");

        result.Should().BeError(ErrorCode.Validation, Resources.QueuedMessageId_InvalidId);
    }

    [Fact]
    public void WhenCreatedWithMessageId_ThenReturnsValue()
    {
        var messageId = CreateMessageId();
        var result = QueuedMessageId.Create(messageId);

        result.Should().BeSuccess();
        result.Value.Identifier.Should().Be(messageId);
    }

    private static string CreateMessageId()
    {
        return new MessageQueueMessageIdFactory().Create("aqueuename");
    }
}