using Common.Extensions;
using FluentAssertions;
using UnitTesting.Common.Validation;
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
        error.Message.Should().Be(Error.NoErrorMessage);
    }

    [Fact]
    public void WhenIsNotValuedParameterAndNull_ThenReturnsError()
    {
        var result = ((string?)null).IsNotValuedParameter("aparametername", "amessage", out var error);

        result.Should().BeTrue();
        error.Code.Should().Be(ErrorCode.Validation);
        error.Message.Should().Be("amessage");
    }

    [Fact]
    public void WhenIsNotValuedParameterAndEmpty_ThenReturnsError()
    {
        var result = string.Empty.IsNotValuedParameter("aparametername", "amessage", out var error);

        result.Should().BeTrue();
        error.Code.Should().Be(ErrorCode.Validation);
        error.Message.Should().Be("amessage");
    }

    [Fact]
    public void WhenIsNotValuedParameterAndValid_ThenReturnsSuccess()
    {
        var result = "avalue".IsNotValuedParameter("aparametername", "amessage", out var error);

        result.Should().BeFalse();
        error.Code.Should().Be(ErrorCode.NoError);
        error.Message.Should().Be(Error.NoErrorMessage);
    }

    [Fact]
    public void WhenPopulateWithNullSource_ThenLeavesTarget()
    {
        var instance = new TestMappingClass();
        instance.PopulateWith((TestMappingClass)null!);

        instance.AStringProperty.Should().Be("adefaultvalue");
        instance.AnIntProperty.Should().Be(1);
        instance.AnBoolProperty.Should().Be(true);
        instance.ADateTimeProperty.Should().BeNear(DateTime.UtcNow);
    }

    [Fact]
    public void WhenPopulateWithWithNonDefaultValues_ThenUpdatesTargetValues()
    {
        var datum = DateTime.Today;
        var instance = new TestMappingClass();
        instance.PopulateWith(new TestMappingClass
        {
            AStringProperty = "avalue",
            AnIntProperty = 99,
            AnBoolProperty = false,
            ADateTimeProperty = datum
        });

        instance.AStringProperty.Should().Be("avalue");
        instance.AnIntProperty.Should().Be(99);
        instance.AnBoolProperty.Should().Be(false);
        instance.ADateTimeProperty.Should().Be(datum);
    }

    [Fact]
    public void WhenPopulateWithWithDefaultValues_ThenUpdatesTargetValues()
    {
        var instance = new TestMappingClass();
        instance.PopulateWith(new TestMappingClass
        {
            AStringProperty = default!,
            AnIntProperty = default,
            AnBoolProperty = default,
            ADateTimeProperty = default
        });

        instance.AStringProperty.Should().Be(default);
        instance.AnIntProperty.Should().Be(default);
        instance.AnBoolProperty.Should().Be(default);
        instance.ADateTimeProperty.Should().Be(default);
    }

    [Fact]
    public void WhenThrowIfNotValuedParameterAndNull_ThenThrows()
    {
        FluentActions.Invoking(() => ((string?)null).ThrowIfNotValuedParameter("aparametername", "amessage"))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("amessage (Parameter 'aparametername')")
            .And.ParamName.Should().Be("aparametername");
    }

    [Fact]
    public void WhenThrowIfNotValuedParameterAndEmpty_ThenThrows()
    {
        FluentActions.Invoking(() => string.Empty.ThrowIfNotValuedParameter("aparametername", "amessage"))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithMessage("amessage (Parameter 'aparametername')")
            .And.ParamName.Should().Be("aparametername");
    }

    [Fact]
    public void WhenThrowIfNotValuedParameterAndValid_ThenReturns()
    {
        FluentActions.Invoking(() => "avalue".ThrowIfNotValuedParameter("aparametername", "amessage"))
            .Should().NotThrow();
    }
}