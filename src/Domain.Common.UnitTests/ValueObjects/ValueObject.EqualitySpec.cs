using System.Collections;
using Common.Extensions;
using Domain.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Common.UnitTests.ValueObjects;

[Trait("Category", "Unit")]
public class ValueObjectEqualitySpec
{
    [Fact]
    public void WhenEqualityWithNullInstanceAndNullInstance_ThenReturnsTrue()
    {
        var instance = new ValueObjectSpec.TestMultiValueObject("", 0, false, DateTime.MinValue);
        var left = (ValueObjectSpec.TestMultiValueObject)null!;
        var right = (ValueObjectSpec.TestMultiValueObject)null!;

        var result = (instance as IEqualityComparer).Equals(left, right);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualityWithNullInstanceAndInstance_ThenReturnsFalse()
    {
        var instance = new ValueObjectSpec.TestMultiValueObject("", 0, false, DateTime.MinValue);
        var left = (ValueObjectSpec.TestMultiValueObject)null!;
        var right = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);

        var result = (instance as IEqualityComparer).Equals(left, right);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualityWithInstanceAndNullInstance_ThenReturnsFalse()
    {
        var instance = new ValueObjectSpec.TestMultiValueObject("", 0, false, DateTime.MinValue);
        var left = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);
        var right = (ValueObjectSpec.TestMultiValueObject)null!;

        var result = (instance as IEqualityComparer).Equals(left, right);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualityWithDifferentTypes_ThenReturnsFalse()
    {
        var instance = new ValueObjectSpec.TestMultiValueObject("", 0, false, DateTime.MinValue);
        var left = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);
        var right = new ValueObjectSpec.TestSingleStringValueObject("avalue");

        var result = (instance as IEqualityComparer).Equals(left, right);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualityWithDifferentTypesThanValueObject_ThenReturnsFalse()
    {
        var instance = new ValueObjectSpec.TestMultiValueObject("", 0, false, DateTime.MinValue);
        var left = new ValueObjectSpec.TestSingleStringValueObject("avalue1");
        var right = new ValueObjectSpec.TestSingleStringValueObject("avalue2");

        var result = (instance as IEqualityComparer).Equals(left, right);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualityWithDifferentValues_ThenReturnsFalse()
    {
        var instance = new ValueObjectSpec.TestMultiValueObject("", 0, false, DateTime.MinValue);
        var left = new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, DateTime.UtcNow);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue2", 25, true, DateTime.UtcNow);

        var result = (instance as IEqualityComparer).Equals(left, right);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualityWithSameValues_ThenReturnsTrue()
    {
        var datum = DateTime.UtcNow;
        var instance = new ValueObjectSpec.TestMultiValueObject("", 0, false, DateTime.MinValue);
        var left = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);

        var result = (instance as IEqualityComparer).Equals(left, right);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualityComparerWithNullInstanceAndNullInstance_ThenReturnsFalse()
    {
        var instance = new ValueObjectSpec.TestMultiValueObject("", 0, false, DateTime.MinValue);
        var left = (ValueObjectSpec.TestMultiValueObject)null!;
        var right = (ValueObjectSpec.TestMultiValueObject)null!;

        var result =
            (instance as IEqualityComparer<ValueObjectBase<ValueObjectSpec.TestMultiValueObject>>).Equals(left, right);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualityComparerWithNullInstanceAndInstance_ThenReturnsFalse()
    {
        var instance = new ValueObjectSpec.TestMultiValueObject("", 0, false, DateTime.MinValue);
        var left = (ValueObjectSpec.TestMultiValueObject)null!;
        var right = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);

        var result =
            (instance as IEqualityComparer<ValueObjectBase<ValueObjectSpec.TestMultiValueObject>>).Equals(left, right);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualityComparerWithInstanceAndNullInstance_ThenReturnsFalse()
    {
        var instance = new ValueObjectSpec.TestMultiValueObject("", 0, false, DateTime.MinValue);
        var left = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);
        var right = (ValueObjectSpec.TestMultiValueObject)null!;

        var result =
            (instance as IEqualityComparer<ValueObjectBase<ValueObjectSpec.TestMultiValueObject>>).Equals(left, right);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualityComparerWithDifferentValues_ThenReturnsFalse()
    {
        var instance = new ValueObjectSpec.TestMultiValueObject("", 0, false, DateTime.MinValue);
        var left = new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, DateTime.UtcNow);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue2", 25, true, DateTime.UtcNow);

        var result =
            (instance as IEqualityComparer<ValueObjectBase<ValueObjectSpec.TestMultiValueObject>>).Equals(left, right);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualityComparerWithSameValues_ThenReturnsTrue()
    {
        var datum = DateTime.UtcNow;
        var instance = new ValueObjectSpec.TestMultiValueObject("", 0, false, DateTime.MinValue);
        var left = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);
        var right = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, datum);

        var result =
            (instance as IEqualityComparer<ValueObjectBase<ValueObjectSpec.TestMultiValueObject>>).Equals(left, right);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsWithNull_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestSingleStringValueObject("avalue").Equals(null!);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithDifferentValue_ThenReturnsFalse()
    {
        var result =
            new ValueObjectSpec.TestSingleStringValueObject("avalue").Equals(
                new ValueObjectSpec.TestSingleStringValueObject("anothervalue"));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithDifferentValueByCase_ThenReturnsFalse()
    {
        var result =
            new ValueObjectSpec.TestSingleStringValueObject("avalue").Equals(
                new ValueObjectSpec.TestSingleStringValueObject("AVALUE"));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithSameValue_ThenReturnsTrue()
    {
        var result =
            new ValueObjectSpec.TestSingleStringValueObject("avalue").Equals(
                new ValueObjectSpec.TestSingleStringValueObject("avalue"));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsWithDifferentValueInMultiValueObject_ThenReturnsFalse()
    {
        var result =
            new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, DateTime.UtcNow).Equals(
                new ValueObjectSpec.TestMultiValueObject("avalue2", 50, false, DateTime.UtcNow));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithNullInMultiValueObject_ThenReturnsFalse()
    {
        var result =
            new ValueObjectSpec.TestMultiValueObject(null!, 25, true, DateTime.UtcNow).Equals(
                new ValueObjectSpec.TestMultiValueObject("avalue2", 50, false, DateTime.UtcNow));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithBothNullInMultiValueObject_ThenReturnsTrue()
    {
        var result =
            // ReSharper disable once EqualExpressionComparison
            new ValueObjectSpec.TestMultiValueObject(null!, 17, true, DateTime.MinValue).Equals(
                new ValueObjectSpec.TestMultiValueObject(null!, 17, true, DateTime.MinValue));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsWithSameValueInMultiValueObject_ThenReturnsTrue()
    {
        var datum = DateTime.UtcNow;
        
        var result =
            // ReSharper disable once EqualExpressionComparison
            new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, datum).Equals(
                new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, datum));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsWithDifferentValueInListValueObject_ThenReturnsFalse()
    {
        var result =
            new ValueObjectSpec.TestSingleListValueObjectValueObject(
                new List<ValueObjectSpec.TestSingleStringValueObject>
                {
                    new("avalue1")
                }).Equals(new ValueObjectSpec.TestSingleListValueObjectValueObject(
                new List<ValueObjectSpec.TestSingleStringValueObject>
                {
                    new("avalue2")
                }));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithDifferentValuesInListValueObject_ThenReturnsFalse()
    {
        var result =
            new ValueObjectSpec.TestSingleListValueObjectValueObject(
                new List<ValueObjectSpec.TestSingleStringValueObject>
                {
                    new("avalue1"),
                    new("avalue2"),
                    new("avalue3")
                }).Equals(new ValueObjectSpec.TestSingleListValueObjectValueObject(
                new List<ValueObjectSpec.TestSingleStringValueObject>
                {
                    new("avalue4"),
                    new("avalue5"),
                    new("avalue6")
                }));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithDifferentValuesByCaseInListValueObject_ThenReturnsFalse()
    {
        var result =
            new ValueObjectSpec.TestSingleListValueObjectValueObject(
                new List<ValueObjectSpec.TestSingleStringValueObject>
                {
                    new("avalue1"),
                    new("avalue2"),
                    new("avalue3")
                }).Equals(new ValueObjectSpec.TestSingleListValueObjectValueObject(
                new List<ValueObjectSpec.TestSingleStringValueObject>
                {
                    new("AVALUE1"),
                    new("AVALUE2"),
                    new("AVALUE3")
                }));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithEmptyValueInListValueObject_ThenReturnsTrue()
    {
        var result =
            // ReSharper disable once EqualExpressionComparison
            new ValueObjectSpec.TestSingleListValueObjectValueObject(
                    new List<ValueObjectSpec.TestSingleStringValueObject>())
                .Equals(new ValueObjectSpec.TestSingleListValueObjectValueObject(
                    new List<ValueObjectSpec.TestSingleStringValueObject>()));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsWithSameValuesInListValueObject_ThenReturnsTrue()
    {
        var result =
            // ReSharper disable once EqualExpressionComparison
            new ValueObjectSpec.TestSingleListValueObjectValueObject(
                new List<ValueObjectSpec.TestSingleStringValueObject>
                {
                    new("avalue1"),
                    new("avalue2"),
                    new("avalue3")
                }).Equals(new ValueObjectSpec.TestSingleListValueObjectValueObject(
                new List<ValueObjectSpec.TestSingleStringValueObject>
                {
                    new("avalue1"),
                    new("avalue2"),
                    new("avalue3")
                }));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsWithNullStringValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow).Equals(null!);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithDifferentStringValue_ThenReturnsFalse()
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        var result =
            new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow).Equals("adifferentvalue");

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsWithSameStringValue_ThenReturnsTrue()
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        var result = new ValueObjectSpec.TestMultiValueObject("astringvalue", 25, true, DateTime.MinValue)
            .Equals(
                "{\"Val1\":\"astringvalue\",\"Val2\":25,\"Val3\":true,\"Val4\":\"0001-01-01T00:00:00\",\"Val5\":\"NULL\",\"Val6\":\"NULL\",\"Val7\":\"NULL\",\"Val8\":\"NULL\"}");

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenOperatorEqualsWithNullString_ThenReturnsFalse()
    {
        var valueObject = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);

        var result = valueObject == (string)null!;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenNotOperatorEqualsWithNullString_ThenReturnsTrue()
    {
        var valueObject = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);

        var result = valueObject != (string)null!;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenOperatorEqualsWithNullInstanceAndNullString_ThenReturnsTrue()
    {
        var result = (ValueObjectSpec.TestMultiValueObject)null! == (string)null!;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenOperatorEqualsWithDifferentString_ThenReturnsFalse()
    {
        var valueObject = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);

        var result = valueObject == "adifferentvalue";

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenOperatorEqualsWithSameString_ThenReturnsTrue()
    {
        var datum = DateTime.UtcNow;
        var valueObject = new ValueObjectSpec.TestMultiValueObject("astringvalue", 25, true, datum);

        var result = valueObject
                     == $"{{\"Val1\":\"astringvalue\",\"Val2\":25,\"Val3\":true,\"Val4\":\"{datum.ToIso8601()}\",\"Val5\":\"NULL\",\"Val6\":\"NULL\",\"Val7\":\"NULL\",\"Val8\":\"NULL\"}}";

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenOperatorEqualsWithNullValue_ThenReturnsFalse()
    {
        var valueObject = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);

        var result = valueObject == (ValueObjectSpec.TestMultiValueObject)null!;

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenNotOperatorEqualsWithNullValue_ThenReturnsTrue()
    {
        var valueObject = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.UtcNow);

        var result = valueObject != (ValueObjectSpec.TestMultiValueObject)null!;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenOperatorEqualsWithNullInstanceAndNullValue_ThenReturnsTrue()
    {
        var result = null! == (ValueObjectSpec.TestMultiValueObject)null!;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenOperatorEqualsWithDifferentValue_ThenReturnsFalse()
    {
        var result = new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, DateTime.UtcNow)
                     == new ValueObjectSpec.TestMultiValueObject("avalue2", 25, true, DateTime.UtcNow);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenOperatorEqualsWithSameValue_ThenReturnsTrue()
    {
        var datum = DateTime.UtcNow;
        
        // ReSharper disable once EqualExpressionComparison
        var result = new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, datum)
                     == new ValueObjectSpec.TestMultiValueObject("avalue1", 25, true, datum);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenOperatorNotEqualsWithNullInstanceAndNullValue_ThenReturnsTrue()
    {
        var result = null! != (ValueObjectSpec.TestMultiValueObject)null!;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenOperatorNotEqualsWithNullInstanceAndVNullValue_ThenReturnsTrue()
    {
        var result = (ValueObjectSpec.TestMultiValueObject)null! != (string)null!;

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenObjectGetHashCode_ThenReturnsHash()
    {
        var valueObject = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.MinValue);

        var result = valueObject.GetHashCode();

        result.Should().Be(934158378);
    }

    [Fact]
    public void WhenEqualityComparerGetHashCode_ThenReturnsHash()
    {
        var instance = new ValueObjectSpec.TestMultiValueObject("", 0, false, DateTime.MinValue);
        var other = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.MinValue);

        var result = (instance as IEqualityComparer).GetHashCode(other);

        result.Should().Be(934158378);
    }

    [Fact]
    public void WhenEqualityComparerGetHashCodeWithOtherType_ThenReturnsHash()
    {
        var instance = new ValueObjectSpec.TestMultiValueObject("", 0, false, DateTime.MinValue);
        var other = new ValueObjectSpec.TestSingleStringValueObject("avalue");

        var result = (instance as IEqualityComparer).GetHashCode(other);

        result.Should().Be(934158386);
    }

    [Fact]
    public void WhenEqualityComparerOfTGetHashCode_ThenReturnsHash()
    {
        var instance = new ValueObjectSpec.TestMultiValueObject("", 0, false, DateTime.MinValue);
        var other = new ValueObjectSpec.TestMultiValueObject("avalue", 25, true, DateTime.MinValue);

        var result = (instance as IEqualityComparer<ValueObjectSpec.TestMultiValueObject>).GetHashCode(other);

        result.Should().Be(934158378);
    }
}