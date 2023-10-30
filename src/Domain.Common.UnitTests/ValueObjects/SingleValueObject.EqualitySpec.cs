using FluentAssertions;
using Xunit;

namespace Domain.Common.UnitTests.ValueObjects;

[Trait("Category", "Unit")]
public class SingleValueObjectEqualitySpec
{
    [Fact]
    public void WhenEqualsOperatorWithNullInstance_ThenReturnsFalse()
    {
        var result = null! == new ValueObjectSpec.TestSingleStringValueObject("avalue");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOperatorWithInstanceAndNullInstance_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") == null!;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOperatorWithSameValue_ThenReturnsTrue()
    {
        // ReSharper disable once EqualExpressionComparison
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue")
                     == new ValueObjectSpec.TestSingleStringValueObject("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsOperatorWithDifferentValues_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue")
                     == new ValueObjectSpec.TestSingleStringValueObject("anothervalue");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOperatorWithNullInstanceAndValue_ThenReturnsFalse()
    {
        var result = (ValueObjectSpec.TestSingleStringValueObject)null! == "avalue";

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOperatorWithInstanceAndNullValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") == (string)null!;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOperatorWithSameStringValue_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") == "avalue";

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsOperatorWithDifferentStringValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") == "anothervalue";

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsOperatorWithSameIntValue_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleIntValueObject(1) == 1;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsOperatorWithDifferentIntValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleIntValueObject(1) == 101;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithNullInstance_ThenReturnsTrue()
    {
        var result = null! != new ValueObjectSpec.TestSingleStringValueObject("avalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhennotEqualsOperatorWithInstanceAndNullInstance_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") != null!;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhennotEqualsOperatorWithSameValue_ThenReturnsFalse()
    {
        // ReSharper disable once EqualExpressionComparison
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue")
                     != new ValueObjectSpec.TestSingleStringValueObject("avalue");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithDifferentValues_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue")
                     != new ValueObjectSpec.TestSingleStringValueObject("anothervalue");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithNullInstanceAndValue_ThenReturnsTrue()
    {
        var result = (ValueObjectSpec.TestSingleStringValueObject)null! != "avalue";

        result.Should().BeTrue();
    }

    [Fact]
    public void WhennotEqualsOperatorWithInstanceAndNullValue_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") != (string)null!;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhennotEqualsOperatorWithNullInstanceValueAndValue_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject(null!) != "avalue";

        result.Should().BeTrue();
    }

    [Fact]
    public void WhennotEqualsOperatorWithSameStringValue_ThenReturnsFalse()
    {
        // ReSharper disable once EqualExpressionComparison
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") != "avalue";

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenNotEqualsOperatorWithDifferentStringValue_ThenReturnsTrue()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue") != "anothervalue";

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenObjectGetHashCodeWithNullValue_ThenReturnsZero()
    {
        var instance = new ValueObjectSpec.TestSingleStringValueObject(null!);

        var result = instance.GetHashCode();

        result.Should().Be(0);
    }

    [Fact]
    public void WhenObjectGetHashCodeWithStringValue_ThenReturnsHash()
    {
        var instance = new ValueObjectSpec.TestSingleStringValueObject("avalue");

        var result = instance.GetHashCode();

        result.Should().Be(934158386);
    }

    [Fact]
    public void WhenObjectGetHashCodeWithIntValue_ThenReturnsHash()
    {
        var instance = new ValueObjectSpec.TestSingleIntValueObject(99);

        var result = instance.GetHashCode();

        result.Should().Be(99);
    }
}