using FluentAssertions;
using UnitTesting.Common.Validation;
using Xunit;

namespace Domain.Common.UnitTests;

[Trait("Category", "Unit")]
public class MessageBusTopicMessageIdFactorySpec
{
    private readonly MessageBusTopicMessageIdFactory _factory;

    public MessageBusTopicMessageIdFactorySpec()
    {
        _factory = new MessageBusTopicMessageIdFactory();
    }

    [Fact]
    public void WhenCreate_ThenCreatesRandomUniqueIdentifier()
    {
        var result1 = _factory.Create("aname");
        var result2 = _factory.Create("aname");
        var result3 = _factory.Create("aname");

        result1.Should().NotBeEmpty();
        result1.Should().NotBe(result2);
        result2.Should().NotBe(result3);
        result3.Should().NotBe(result1);
    }

    [Fact]
    public void WhenCreateAndQueueNameIsTooShort_ThenThrows()
    {
        _factory
            .Invoking(x => x.Create("a"))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.MessageBusTopicMessageIdFactory_InvalidTopicName);
    }

    [Fact]
    public void WhenCreateAndQueueNameIsTooLong_ThenThrows()
    {
        _factory
            .Invoking(x => x.Create(new string('a', MessageBusTopicMessageIdFactory.MaxTopicName + 1)))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.MessageBusTopicMessageIdFactory_InvalidTopicName);
    }

    [Fact]
    public void WhenCreateAndQueueNameIsUnderscored_ThenCreates()
    {
        var result = _factory.Create("a_name");

        result.Should().StartWith("a_name_");
    }

    [Fact]
    public void WhenIsValidForIncompleteId_ThenReturnsFalse()
    {
        var id = "anid";

        var result = _factory.IsValid(id);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidForJustGuid_ThenReturnsFalse()
    {
        var id = Guid.NewGuid().ToString("N");

        var result = _factory.IsValid(id);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidForTooLongQueueName_ThenReturnsFalse()
    {
        var id = $"a_{Guid.NewGuid():N}";

        var result = _factory.IsValid(id);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidWhenDelimiterIsMissing_ThenReturnsFalse()
    {
        var id = $"aqueuename{Guid.NewGuid():N}";

        var result = _factory.IsValid(id);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenIsValidForCreatedId_ThenReturnsTrue()
    {
        var id = _factory.Create("aname");

        var result = _factory.IsValid(id);

        result.Should().BeTrue();
    }
}