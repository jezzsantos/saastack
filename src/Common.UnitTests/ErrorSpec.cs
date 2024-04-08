using FluentAssertions;
using Xunit;
using Environment = System.Environment;

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

    [Fact]
    public void WhenWrapWithCodeOnNoErrorWithNoMessage_ThenReturnsNewError()
    {
        var result = Error.NoError.Wrap(ErrorCode.Unexpected, "");

        result.Code.Should().Be(ErrorCode.Unexpected);
        result.Message.Should().Be(Error.NoErrorMessage);
    }

    [Fact]
    public void WhenWrapWithCodeOnNoErrorWithMessage_ThenReturnsNewError()
    {
        var result = Error.NoError.Wrap(ErrorCode.Unexpected, "amessage");

        result.Code.Should().Be(ErrorCode.Unexpected);
        result.Message.Should().Be($"{nameof(ErrorCode.NoError)}: amessage");
    }

    [Fact]
    public void WhenWrapWithCodeOnAnyErrorWithMessage_ThenReturnsNewError()
    {
        var result = Error.RuleViolation("aruleviolation")
            .Wrap(ErrorCode.Unexpected, "amessage");

        result.Code.Should().Be(ErrorCode.Unexpected);
        result.Message.Should().Be($"{nameof(ErrorCode.RuleViolation)}: amessage{Environment.NewLine}\taruleviolation");
    }

    [Fact]
    public void WhenWrapWithCodeOnAnyErrorWithMessageAgain_ThenReturnsNewError()
    {
        var result = Error.RuleViolation("aruleviolation")
            .Wrap(ErrorCode.EntityExists, "amessage")
            .Wrap(ErrorCode.Unexpected, "anothermessage");

        result.Code.Should().Be(ErrorCode.Unexpected);
        result.Message.Should()
            .Be(
                $"{nameof(ErrorCode.EntityExists)}: anothermessage{Environment.NewLine}\t{nameof(ErrorCode.RuleViolation)}: amessage{Environment.NewLine}\taruleviolation");
    }
}