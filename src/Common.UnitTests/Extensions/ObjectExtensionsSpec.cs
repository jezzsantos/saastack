using Common.Extensions;
using FluentAssertions;
using Xunit;

namespace Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class ObjectExtensionsSpec
{
    [Fact]
    public void WhenIsInvalidParameterAndInvalidWithNoMessage_ThenReturnsError()
    {
        var result = "avalue".IsInvalidParameter(s => s.Length > 99, "aparametername", null, out var error);

        result.Should().BeTrue();
        error.Code.Should().Be(ErrorCode.Validation);
        error.Message.Should().Be("aparametername");
    }

    [Fact]
    public void WhenIsInvalidParameterAndInvalidWithMessage_ThenReturnsError()
    {
        var result = "avalue".IsInvalidParameter(s => s.Length > 99, "aparametername", "amessage", out var error);

        result.Should().BeTrue();
        error.Code.Should().Be(ErrorCode.Validation);
        error.Message.Should().Be("amessage");
    }

    [Fact]
    public void WhenIsInvalidParameterAndValid_ThenReturnsSuccess()
    {
        var result = "avalue".IsInvalidParameter(s => s.Length < 99, "aparametername", null, out var error);

        result.Should().BeFalse();
        error.Code.Should().Be(ErrorCode.NoError);
        error.Message.Should().BeNull();
    }

    [Fact]
    public void WhenIsValuedParameterAndNull_ThenReturnsError()
    {
        var result = ((string?)null).IsNotValuedParameter("aparametername", "amessage", out var error);

        result.Should().BeTrue();
        error.Code.Should().Be(ErrorCode.Validation);
        error.Message.Should().Be("amessage");
    }

    [Fact]
    public void WhenIsValuedParameterAndEmpty_ThenReturnsError()
    {
        var result = string.Empty.IsNotValuedParameter("aparametername", "amessage", out var error);

        result.Should().BeTrue();
        error.Code.Should().Be(ErrorCode.Validation);
        error.Message.Should().Be("amessage");
    }

    [Fact]
    public void WhenIsValuedParameterAndValid_ThenReturnsSuccess()
    {
        var result = "avalue".IsNotValuedParameter("aparametername", "amessage", out var error);

        result.Should().BeFalse();
        error.Code.Should().Be(ErrorCode.NoError);
        error.Message.Should().BeNull();
    }
}