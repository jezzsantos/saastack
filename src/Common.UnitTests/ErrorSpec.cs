using FluentAssertions;
using Xunit;

namespace Common.UnitTests;

[Trait("Category", "Unit")]
public class ErrorSpec
{
    [Fact]
    public void WhenConstructed_ThenReturnsError()
    {
        var result = new Error();

        result.Code.Should().Be(ErrorCode.NoError);
        result.Message.Should().Be(Error.NoErrorMessage);
    }

    [Fact]
    public void WhenConstructedWithCode_ThenReturnsError()
    {
        var result = Error.EntityExists();

        result.Code.Should().Be(ErrorCode.EntityExists);
        result.Message.Should().Be(Error.NoErrorMessage);
    }

    [Fact]
    public void WhenWrapOnNoErrorWithNoMessage_ThenReturnsNewError()
    {
        var result = Error.NoError.Wrap("");

        result.Code.Should().Be(ErrorCode.NoError);
        result.Message.Should().Be(Error.NoErrorMessage);
    }

    [Fact]
    public void WhenWrapOnNoErrorWithMessage_ThenReturnsNewError()
    {
        var result = Error.NoError.Wrap("amessage");

        result.Code.Should().Be(ErrorCode.NoError);
        result.Message.Should().Be("amessage");
    }

    [Fact]
    public void WhenWrapOnAnyErrorWithMessage_ThenReturnsNewError()
    {
        var result = Error.RuleViolation("aruleviolation")
            .Wrap("amessage");

        result.Code.Should().Be(ErrorCode.RuleViolation);
        result.Message.Should().Be($"amessage{Environment.NewLine}\taruleviolation");
    }

    [Fact]
    public void WhenWrapOnAnyErrorWithMessageAgain_ThenReturnsNewError()
    {
        var result = Error.RuleViolation("aruleviolation")
            .Wrap("amessage")
            .Wrap("anothermessage");

        result.Code.Should().Be(ErrorCode.RuleViolation);
        result.Message.Should()
            .Be($"anothermessage{Environment.NewLine}\tamessage{Environment.NewLine}\taruleviolation");
    }
}