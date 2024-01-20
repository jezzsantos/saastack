using FluentAssertions;
using UnitTesting.Common.Validation;
using Xunit;

namespace Domain.Common.UnitTests;

[Trait("Category", "Unit")]
public class MessageQueueIdFactorySpec
{
    private readonly MessageQueueIdFactory _factory;

    public MessageQueueIdFactorySpec()
    {
        _factory = new MessageQueueIdFactory();
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
            .WithMessageLike(Resources.MessageQueueFactory_InvalidQueueName);
    }

    [Fact]
    public void WhenCreateAndQueueNameIsTooLong_ThenThrows()
    {
        _factory
            .Invoking(x => x.Create(new string('a', MessageQueueIdFactory.MaxQueueName + 1)))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessageLike(Resources.MessageQueueFactory_InvalidQueueName);
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