using FluentAssertions;
using Xunit;

namespace Domain.Common.UnitTests.ValueObjects;

[Trait("Category", "Unit")]
public class ValueObjectComparableSpec
{
    [Fact]
    public void WhenCompareToWithNullInstance_ThenReturnsBefore()
    {
        var left = new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, DateTime.UtcNow);
        var right = (ValueObjectSpec.TestMultiValueObject)null!;

        var result = (left as IComparable).CompareTo(right);

        result.Should().Be(-1);
    }

    [Fact]
    public void WhenCompareToWithWrongType_ThenReturnsBefore()
    {
        var left = new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, DateTime.UtcNow);
        var right = new ValueObjectSpec.TestSingleStringValueObject("avalue");

        var result = (left as IComparable).CompareTo(right);

        result.Should().Be(-1);
    }

    [Fact]
    public void WhenCompareToWithDifferentValues_ThenReturnsBefore()
    {
        var left = new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, DateTime.UtcNow);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue2", 0, false, DateTime.UtcNow);

        var result = (left as IComparable).CompareTo(right);

        result.Should().Be(-1);
    }

    [Fact]
    public void WhenCompareToWithSameValues_ThenReturnsEqual()
    {
        var datum = DateTime.UtcNow;
        var left = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);

        var result = (left as IComparable).CompareTo(right);

        result.Should().Be(0);
    }

    [Fact]
    public void WhenCompareToTWithNullInstance_ThenReturnsUnComparable()
    {
        var left = new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, DateTime.UtcNow);
        var right = (ValueObjectSpec.TestMultiValueObject)null!;

        var result = (left as IComparable<ValueObjectSpec.TestMultiValueObject>).CompareTo(right);

        result.Should().Be(-1);
    }

    [Fact]
    public void WhenCompareToTWithDifferentValues_ThenReturnsBefore()
    {
        var left = new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, DateTime.UtcNow);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue2", 0, false, DateTime.UtcNow);

        var result = (left as IComparable<ValueObjectSpec.TestMultiValueObject>).CompareTo(right);

        result.Should().Be(-1);
    }

    [Fact]
    public void WhenCompareToTWithSameValues_ThenReturnsSame()
    {
        var datum = DateTime.UtcNow;
        var left = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);

        var result = (left as IComparable<ValueObjectSpec.TestMultiValueObject>).CompareTo(right);

        result.Should().Be(0);
    }

    [Fact]
    public void WhenGreaterThanOperatorWithNullInstanceAndInstance_ThenReturnsFalse()
    {
        var left = (ValueObjectSpec.TestMultiValueObject)null!;
        var right = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);

        var result = left > right;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanOperatorWithInstanceAndNullInstance_ThenReturnsFalse()
    {
        var left = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);
        var right = (ValueObjectSpec.TestMultiValueObject)null!;

        var result = left > right;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanOperatorWithDifferentValues_ThenReturnsTrue()
    {
        var left = new ValueObjectSpec.TestMultiValueObject("avalue2", 0, false, DateTime.UtcNow);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, DateTime.UtcNow);

        var result = left > right;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenGreaterThanOperatorWithSameValues_ThenReturnsFalse()
    {
        var datum = DateTime.UtcNow;
        var left = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);

        var result = left > right;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanOrEqualOperatorWithNullInstanceAndInstance_ThenReturnsFalse()
    {
        var left = (ValueObjectSpec.TestMultiValueObject)null!;
        var right = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);

        var result = left >= right;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanOrEqualOperatorWithInstanceAndNullInstance_ThenReturnsFalse()
    {
        var left = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);
        var right = (ValueObjectSpec.TestMultiValueObject)null!;

        var result = left >= right;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenGreaterThanOrEqualOperatorWithDifferentValues_ThenReturnsTrue()
    {
        var left = new ValueObjectSpec.TestMultiValueObject("avalue2", 0, false, DateTime.UtcNow);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, DateTime.UtcNow);

        var result = left >= right;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenGreaterThanOrEqualOperatorWithSameValues_ThenReturnsTrue()
    {
        var datum = DateTime.UtcNow;
        var left = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);

        var result = left >= right;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenLessThanOperatorWithNullInstanceAndInstance_ThenReturnsFalse()
    {
        var left = (ValueObjectSpec.TestMultiValueObject)null!;
        var right = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);

        var result = left < right;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLessThanOperatorWithInstanceAndNullInstance_ThenReturnsTrue()
    {
        var left = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);
        var right = (ValueObjectSpec.TestMultiValueObject)null!;

        var result = left < right;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenLessThanOperatorWithDifferentValues_ThenReturnsTrue()
    {
        var left = new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, DateTime.UtcNow);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue2", 0, false, DateTime.UtcNow);

        var result = left < right;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenLessThanOperatorWithSameValues_ThenReturnsFalse()
    {
        var datum = DateTime.UtcNow;
        var left = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);

        var result = left < right;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLessThanOrEqualOperatorWithNullInstanceAndInstance_ThenReturnsFalse()
    {
        var left = (ValueObjectSpec.TestMultiValueObject)null!;
        var right = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);

        var result = left <= right;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenLessThanOrEqualOperatorWithInstanceAndNullInstance_ThenReturnsTrue()
    {
        var left = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);
        var right = (ValueObjectSpec.TestMultiValueObject)null!;

        var result = left <= right;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenLessThanOrEqualOperatorWithDifferentValues_ThenReturnsTrue()
    {
        var left = new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, DateTime.UtcNow);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue2", 0, false, DateTime.UtcNow);

        var result = left <= right;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenLessThanOrEqualOperatorWithSameValues_ThenReturnsTrue()
    {
        var datum = DateTime.UtcNow;
        var left = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);

        var result = left <= right;

        result.Should().BeTrue();
    }
}