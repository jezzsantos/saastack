using Common.Extensions;
using FluentAssertions;
using Xunit;

namespace Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class EnumExtensionsSpec
{
    [Fact]
    public void WhenToEnumAndEmpty_ThenThrows()
    {
        ""
            .Invoking(x => x.ToEnum<SourceEnum>())
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WhenToEnumAndNotMatches_ThenThrows()
    {
        "notavalue"
            .Invoking(x => x.ToEnum<SourceEnum>())
            .Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WhenToEnumAndMatches_ThenReturnsMatched()
    {
        var result = SourceEnum.OptionOne.ToString().ToEnum<SourceEnum>();

        result.Should().Be(SourceEnum.OptionOne);
    }

    [Fact]
    public void WhenToEnumOrDefaultAndEmpty_ThenReturnsDefault()
    {
        var result = "".ToEnumOrDefault(SourceEnum.OptionTwo);

        result.Should().Be(SourceEnum.OptionTwo);
    }

    [Fact]
    public void WhenToEnumOrDefaultAndNotMatches_ThenReturnsDefault()
    {
        var result = "notavalue".ToEnumOrDefault(SourceEnum.OptionTwo);

        result.Should().Be(SourceEnum.OptionTwo);
    }

    [Fact]
    public void WhenToEnumOrDefaultAndMatches_ThenReturnsMatched()
    {
        var result = SourceEnum.OptionOne.ToString().ToEnumOrDefault(SourceEnum.OptionTwo);

        result.Should().Be(SourceEnum.OptionOne);
    }


    [Fact]
    public void WhenToEnumAndHasSameOption_ThenReturnsOption()
    {
        var result = SourceEnum.OptionTwo.ToEnum<SourceEnum, TargetEnum>();

        result.Should().Be(TargetEnum.OptionTwo);
    }

    [Fact]
    public void WhenToEnumAndMissingOptionInTarget_ThenThrows()
    {
        SourceEnum.OptionOne
            .Invoking(x => x.ToEnum<SourceEnum, TargetEnum>())
            .Should().Throw<ArgumentException>()
            .WithMessage($"Requested value '{SourceEnum.OptionOne}' was not found.");
    }

    [Fact]
    public void WhenToEnumOrDefaultAndSameOption_ThenReturnsOption()
    {
        var result = SourceEnum.OptionTwo.ToEnumOrDefault(TargetEnum.OptionThree);

        result.Should().Be(TargetEnum.OptionTwo);
    }

    [Fact]
    public void WhenToEnumOrDefaultAndOptionNotExistsInTarget_ThenReturnsDefault()
    {
        var result = SourceEnum.OptionOne.ToEnumOrDefault(TargetEnum.OptionThree);

        result.Should().Be(TargetEnum.OptionThree);
    }
}

#pragma warning disable S2344
internal enum SourceEnum
#pragma warning restore S2344
{
    OptionOne,
    OptionTwo
}

#pragma warning disable S2344
internal enum TargetEnum
#pragma warning restore S2344
{
    OptionTwo,
    OptionThree
}