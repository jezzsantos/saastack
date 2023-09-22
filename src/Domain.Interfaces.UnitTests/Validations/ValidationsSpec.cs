using Domain.Interfaces.Validations;
using FluentAssertions;
using Xunit;

namespace Domain.Interfaces.UnitTests.Validations;

[Trait("Category", "Unit")]
public class ValidationsSpec
{
    [Fact]
    public void WhenMatchesHasFunction_ThenReturnsTrue()
    {
        var validationFormat = new Validation(_ => true);

        var result = validationFormat.Matches("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenMatchesHasFunction_ThenReturnsFalse()
    {
        var validationFormat = new Validation(_ => false);

        var result = validationFormat.Matches("avalue");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenMatchesHasExpression_ThenReturnsTrue()
    {
        var validationFormat = new Validation(@"^avalue$");

        var result = validationFormat.Matches("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenMatchesHasExpressionAndIsNotTooLong_ThenReturnsTrue()
    {
        var validationFormat = new Validation(@"^a*$", 1, 10);

        var result = validationFormat.Matches("aaaaaaa");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenMatchesHasExpressionAndIsTooLong_ThenReturnsFalse()
    {
        var validationFormat = new Validation(@"^a*$", 1, 1);

        var result = validationFormat.Matches("aaaaaaa");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEmailWithNoName_ThenReturnsFalse()
    {
        var result = CommonValidations.EmailAddress.Matches("@company.com");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEmailWithWhitespaceName_ThenReturnsFalse()
    {
        var result = CommonValidations.EmailAddress.Matches(" @company.com");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEmailWithCommonFormat_ThenReturnsTrue()
    {
        var result = CommonValidations.EmailAddress.Matches("aname@acompany.com");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEmailWithMultiLevelDomainFormat_ThenReturnsTrue()
    {
        var result = CommonValidations.EmailAddress.Matches("aname@anaustraliancompany.com.au");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenDescriptiveNameWithTooShort_ThenReturnsFalse()
    {
        var result = CommonValidations.DescriptiveName(2, 10).Matches("a");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenDescriptiveNameIsEmpty_ThenReturnsFalse()
    {
        var result = CommonValidations.DescriptiveName().Matches("");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenDescriptiveNameWithTooLong_ThenReturnsFalse()
    {
        var result = CommonValidations.DescriptiveName(2, 10).Matches("atoolongstring");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenDescriptiveNameWithInvalidPrintableChar_ThenReturnsFalse()
    {
        var result = CommonValidations.DescriptiveName(2, 10).Matches("^aninvalidstring");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenDescriptiveNameWithLeastChars_ThenReturnsTrue()
    {
        var result = CommonValidations.DescriptiveName(6, 10).Matches("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenDescriptiveNameWithMaxChars_ThenReturnsTrue()
    {
        var result = CommonValidations.DescriptiveName(2, 6).Matches("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenDescriptiveNameWithValidChars_ThenReturnsTrue()
    {
        var result = CommonValidations.DescriptiveName().Matches("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenFreeFormTextWithTooShort_ThenReturnsFalse()
    {
        var result = CommonValidations.FreeformText(2, 10).Matches("a");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenFreeFormTextWithTooLong_ThenReturnsFalse()
    {
        var result = CommonValidations.FreeformText(2, 10).Matches("atoolongstring");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenFreeFormTextWithInvalidPrintableChar_ThenReturnsFalse()
    {
        var result = CommonValidations.FreeformText(2, 10).Matches("^aninvalidstring");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenFreeFormTextWithLeastChars_ThenReturnsTrue()
    {
        var result = CommonValidations.FreeformText(6, 10).Matches("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenFreeFormTextWithMaxChars_ThenReturnsTrue()
    {
        var result = CommonValidations.FreeformText(2, 6).Matches("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenFreeFormTextWithValidChars_ThenReturnsTrue()
    {
        var result = CommonValidations.FreeformText().Matches("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenFreeFormTextWithMultiLineInWindows_ThenReturnsTrue()
    {
        var result = CommonValidations.FreeformText().Matches("\r\naline1\r\naline2\r\n");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenFreeFormTextWithMultiLineInUnix_ThenReturnsTrue()
    {
        var result = CommonValidations.FreeformText().Matches("\raline1\raline2\r");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenFreeFormTextWithZeroMinAndEmpty_ThenReturnsTrue()
    {
        var result = CommonValidations.FreeformText(0, 10).Matches("");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenAnythingWithTooShort_ThenReturnsFalse()
    {
        var result = CommonValidations.Anything(2, 10).Matches("a");
        result.Should().BeFalse();
    }

    [Fact]
    public void WhenAnythingWithTooLong_ThenReturnsFalse()
    {
        var result = CommonValidations.Anything(2, 10).Matches("atoolongstring");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenAnythingWithLeastChars_ThenReturnsTrue()
    {
        var result = CommonValidations.Anything(6, 10).Matches("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenAnythingWithMaxChars_ThenReturnsTrue()
    {
        var result = CommonValidations.Anything(2, 6).Matches("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenAnythingWithSpecialCharacters_ThenReturnsTrue()
    {
        var result = CommonValidations.Anything().Matches("atext^是⎈𐂯؄💩⚡");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenMarkdownTextWithTooShort_ThenReturnsFalse()
    {
        var result = CommonValidations.Markdown(2, 10).Matches("a");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenMarkdownTextWithTooLong_ThenReturnsFalse()
    {
        var result = CommonValidations.Markdown(2, 10).Matches("atoolongstring");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenMarkdownTextWithInvalidPrintableChar_ThenReturnsFalse()
    {
        var result = CommonValidations.Markdown(2, 10).Matches("^aninvalidstring");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenMarkdownTextWithLeastChars_ThenReturnsTrue()
    {
        var result = CommonValidations.Markdown(6, 10).Matches("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenMarkdownTextWithMaxChars_ThenReturnsTrue()
    {
        var result = CommonValidations.Markdown(2, 6).Matches("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenMarkdownTextWithValidChars_ThenReturnsTrue()
    {
        var result = CommonValidations.Markdown().Matches("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenMarkdownTextWithMultiLineInWindows_ThenReturnsTrue()
    {
        var result = CommonValidations.Markdown().Matches("\r\naline1\r\naline2\r\n");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenMarkdownTextWithMultiLineInUnix_ThenReturnsTrue()
    {
        var result = CommonValidations.Markdown().Matches("\raline1\raline2\r");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenMarkdownTextWithZeroMinAndEmpty_ThenReturnsTrue()
    {
        var result = CommonValidations.Markdown(0, 10).Matches("");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenMarkdownWithValidChars_ThenReturnsTrue()
    {
        var result = CommonValidations.Markdown().Matches("avalue😛");

        result.Should().BeTrue();
    }
}