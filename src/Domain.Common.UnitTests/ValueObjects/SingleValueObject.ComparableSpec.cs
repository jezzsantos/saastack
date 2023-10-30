using FluentAssertions;
using Xunit;

namespace Domain.Common.UnitTests.ValueObjects;

[Trait("Category", "Unit")]
public class SingleValueObjectComparableSpec
{
    [Fact]
    public void WhenGreaterThanWithNullInstanceAndInstance_ThenReturnsFalse()
    {
        var result = null! > new ValueObjectSpec.TestSingleStringValueObject("avalue");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanWithInstanceAndNullInstance_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") > null!;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanSameValue_ThenReturnsFalse()
    {
        // ReSharper disable once EqualExpressionComparison
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue")
                     > new ValueObjectSpec.TestSingleStringValueObject("avalue");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanLargerValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue1")
                     > new ValueObjectSpec.TestSingleStringValueObject("avalue2");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanSmallerValue_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue2")
                     > new ValueObjectSpec.TestSingleStringValueObject("avalue1");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenGreaterThanWithNullInstanceAndStringValue_ThenReturnsFalse()
    {
        var result = (ValueObjectSpec.TestSingleStringValueObject)null! > "avalue";

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanWithNullStringValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") > (string)null!;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanSameStringValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") > "avalue";

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanLargerStringValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue1") > "avalue2";

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanSmallerStringValue_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue2") > "avalue1";

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenGreaterThanOrEqualWithNullInstanceAndInstance_ThenReturnsFalse()
    {
        var result = null! >= new ValueObjectSpec.TestSingleStringValueObject("avalue");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanOrEqualWithInstanceAndNullInstance_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") >= null!;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanOrEqualSameValue_ThenReturnsTrue()
    {
        // ReSharper disable once EqualExpressionComparison
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue")
                     >= new ValueObjectSpec.TestSingleStringValueObject("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenGreaterThanOrEqualLargerValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue1")
                     >= new ValueObjectSpec.TestSingleStringValueObject("avalue2");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanOrEqualSmallerValue_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue2")
                     >= new ValueObjectSpec.TestSingleStringValueObject("avalue1");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenGreaterThanOrEqualWithNullInstanceAndStringValue_ThenReturnsFalse()
    {
        var result = (ValueObjectSpec.TestSingleStringValueObject)null! >= "avalue";

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanOrEqualWithNullStringValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") >= (string)null!;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanOrEqualSameStringValue_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") >= "avalue";

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenGreaterThanOrEqualLargerStringValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue1") >= "avalue2";

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanOrEqualSmallerStringValue_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue2") >= "avalue1";

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenLesserThanWithNullInstanceAndInstance_ThenReturnsFalse()
    {
        var result = null! < new ValueObjectSpec.TestSingleStringValueObject("avalue");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLesserThanWithInstanceAndNullInstance_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") < null!;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLesserThanSameValue_ThenReturnsFalse()
    {
        // ReSharper disable once EqualExpressionComparison
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue")
                     < new ValueObjectSpec.TestSingleStringValueObject("avalue");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLesserThanLargerValue_ThenReturnsFalseTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue1")
                     < new ValueObjectSpec.TestSingleStringValueObject("avalue2");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenLesserThanSmallerValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue2")
                     < new ValueObjectSpec.TestSingleStringValueObject("avalue1");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLesserThanWithNullInstanceAndStringValue_ThenReturnsFalse()
    {
        var result = (ValueObjectSpec.TestSingleStringValueObject)null! < "avalue";

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLesserThanWithNullStringValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") < (string)null!;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLesserThanSameStringValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") < "avalue";

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLesserThanLargerStringValue_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue1") < "avalue2";

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenLesserThanSmallerStringValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue2") < "avalue1";

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLesserThanOrEqualWithNullInstanceAndInstance_ThenReturnsFalse()
    {
        var result = null! <= new ValueObjectSpec.TestSingleStringValueObject("avalue");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLesserThanOrEqualWithInstanceAndNullInstance_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") <= null!;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLesserThanOrEqualSameValue_ThenReturnsTrue()
    {
        // ReSharper disable once EqualExpressionComparison
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue")
                     <= new ValueObjectSpec.TestSingleStringValueObject("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenLesserThanOrEqualLargerValue_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue1")
                     <= new ValueObjectSpec.TestSingleStringValueObject("avalue2");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenLesserThanOrEqualSmallerValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue2")
                     <= new ValueObjectSpec.TestSingleStringValueObject("avalue1");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLesserThanOrEqualWithNullInstanceAndStringValue_ThenReturnsFalse()
    {
        var result = (ValueObjectSpec.TestSingleStringValueObject)null! <= "avalue";

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLesserThanOrEqualWithNullStringValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") <= (string)null!;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLesserThanOrEqualSameStringValue_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") <= "avalue";

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenLesserThanOrEqualLargerStringValue_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue1") <= "avalue2";

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenLesserThanOrEqualSmallerStringValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue2") <= "avalue1";

        result.Should().BeFalse();
    }
}