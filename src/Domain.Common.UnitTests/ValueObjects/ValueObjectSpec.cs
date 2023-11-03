using Common.Extensions;
using Domain.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace Domain.Common.UnitTests.ValueObjects;

[Trait("Category", "Unit")]
public class ValueObjectSpec
{
    [Fact]
    public void WhenDehydrateNoPropertyValues_ThenReturnsProperties()
    {
        var result = new TestMultiValueObjectWithNoValues().Dehydrate();

        result.Should().Be(ValueObjectBase<TestMultiValueObjectWithNoValues>.NullValue);
    }

    [Fact]
    public void WhenDehydrateSinglePropertyValue_ThenReturnsProperties()
    {
        var result = new TestSingleStringValueObject("avalue").Dehydrate();

        result.Should().Be("avalue");
    }

    [Fact]
    public void WhenDehydrateMultiPropertyValueWithNulls_ThenReturnsProperties()
    {
        var result = new TestMultiValueObject(null!, 25, true).Dehydrate();

        result.Should().Be("{\"Val1\":\"NULL\",\"Val2\":25,\"Val3\":true}");
    }

    [Fact]
    public void WhenDehydrateMultiPropertyValue_ThenReturnsProperties()
    {
        var result = new TestMultiValueObject("astringvalue", 25, true).Dehydrate();

        result.Should().Be("{\"Val1\":\"astringvalue\",\"Val2\":25,\"Val3\":true}");
    }

    [Fact]
    public void WhenDehydrateSingleListStringValue_ThenReturnsProperties()
    {
        var value = new List<string>
        {
            "avalue1",
            "avalue2"
        };

        var result = new TestSingleListStringValueObject(value).Dehydrate();

        result.Should().Be("[\"avalue1\",\"avalue2\"]");
    }

    [Fact]
    public void WhenDehydrateSingleListValueObjectValueWithNullItems_ThenThrows()
    {
        var value = new List<TestSingleStringValueObject>
        {
            null!,
            new("avalue2")
        };

        var result = new TestSingleListValueObjectValueObject(value).Dehydrate();

        result.Should().Be("[null,\"avalue2\"]");
    }

    [Fact]
    public void WhenDehydrateSingleListValueObjectValue_ThenReturnsProperties()
    {
        var value = new List<TestSingleStringValueObject>
        {
            new("avalue1"),
            new("avalue2")
        };

        var result = new TestSingleListValueObjectValueObject(value).Dehydrate();

        result.Should().Be("[\"avalue1\",\"avalue2\"]");
    }

    [Fact]
    public void WhenRehydrateMultiValueWithNullValue_ThenReturnsInstance()
    {
        var valueObject = TestMultiValueObject.Rehydrate()("{\"Val1\":\"NULL\",\"Val2\":25,\"Val3\":true}", null!);

        valueObject.AStringValue.Should().BeNull();
        valueObject.AnIntegerValue.Should().Be(25);
        valueObject.ABooleanValue.Should().BeTrue();
    }

    [Fact]
    public void WhenRehydrateMultiValue_ThenReturnsInstance()
    {
        var valueObject =
            TestMultiValueObject.Rehydrate()("{\"Val1\":\"astringvalue\",\"Val2\":25,\"Val3\":true}", null!);

        valueObject.AStringValue.Should().Be("astringvalue");
        valueObject.AnIntegerValue.Should().Be(25);
        valueObject.ABooleanValue.Should().BeTrue();
    }

    [Fact]
    public void WhenRehydrateToListWithNullValue_ThenReturnsEmpty()
    {
        var result = TestMultiValueObject.RehydrateToList(null!, false);

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenRehydrateToListWithEmptyValue_ThenReturnsEmpty()
    {
        var result = TestMultiValueObject.RehydrateToList("", false);

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenRehydrateToListWithSomeValues_ThenReturnsValues()
    {
        var result = TestMultiValueObject.RehydrateToList("{\"Val1\":\"NULL\",\"Val2\":25,\"Val3\":true}", false);

        result.Should().ContainInOrder(null, "25", "True");
    }

    [Fact]
    public void WhenRehydrateToListWithStringValue_ThenReturnsValues()
    {
        var result =
            TestMultiValueObject.RehydrateToList("{\"Val1\":\"astringvalue\",\"Val2\":25,\"Val3\":true}", false);

        result.Should().ContainInOrder("astringvalue", "25", "True");
    }

    [Fact]
    public void WhenRehydrateToListWithNullValueForSingleValue_ThenReturnsEmpty()
    {
        var result = TestMultiValueObject.RehydrateToList(null!, true);

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenRehydrateToListWithEmptyValueForSingleValue_ThenReturnsValue()
    {
        var result = TestMultiValueObject.RehydrateToList("", true);

        result.Should().ContainInOrder("");
    }

    [Fact]
    public void WhenRehydrateToListWithSpecialNullValueForSingleValue_ThenReturnsEmpty()
    {
        var result = TestMultiValueObject.RehydrateToList(ValueObjectBase<TestMultiValueObject>.NullValue, true);

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenRehydrateToListWithStringValueForSingleValue_ThenReturnsValue()
    {
        var result = TestMultiValueObject.RehydrateToList("astringvalue", true);

        result.Should().ContainInOrder("astringvalue");
    }

    [Fact]
    public void WhenRehydrateToListWithNullValueForSingleListValue_ThenReturnsEmpty()
    {
        var result = TestMultiValueObject.RehydrateToList(null!, true, true);

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenRehydrateToListWithEmptyValueForSingleListValue_ThenReturnsEmpty()
    {
        var result = TestMultiValueObject.RehydrateToList("", true, true);

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenRehydrateToListWithSpecialNullValueForSingleListValue_ThenReturnsEmpty()
    {
        var result = TestMultiValueObject.RehydrateToList("[\"NULL\",\"astringvalue\"]", true, true);

        result.Should().ContainInOrder("astringvalue");
    }

    [Fact]
    public void WhenRehydrateToListWithStringValueForSingleListValue_ThenReturnsValue()
    {
        var result = TestMultiValueObject.RehydrateToList("[\"astringvalue\",\"25\",\"true\"]", true, true);

        result.Should().ContainInOrder("astringvalue", "25", "true");
    }

    public sealed class TestSingleListStringValueObject : SingleValueObjectBase<TestSingleListStringValueObject,
        List<string>>
    {
        public TestSingleListStringValueObject(List<string> value) : base(value)
        {
        }

        public static ValueObjectFactory<TestSingleListStringValueObject> Rehydrate()
        {
            return (property, _) => new TestSingleListStringValueObject(property.FromJson<List<string>>()!);
        }

        public List<string> Values => Value;
    }

    public sealed class TestSingleStringValueObject : SingleValueObjectBase<TestSingleStringValueObject, string>
    {
        public TestSingleStringValueObject(string value) : base(value)
        {
        }

        public static ValueObjectFactory<TestSingleStringValueObject> Rehydrate()
        {
            return (property, _) => new TestSingleStringValueObject(property);
        }

        public string StringValue => Value;
    }

    public sealed class TestSingleIntValueObject : SingleValueObjectBase<TestSingleIntValueObject, int>
    {
        public TestSingleIntValueObject(int value) : base(value)
        {
        }

        public static ValueObjectFactory<TestSingleStringValueObject> Rehydrate()
        {
            return (property, _) => new TestSingleStringValueObject(property);
        }

        public int IntValue => Value;
    }

    public sealed class TestSingleEnumValueObject : SingleValueObjectBase<TestSingleEnumValueObject, TestEnum>
    {
        public TestSingleEnumValueObject(TestEnum value) : base(value)
        {
        }

        public static ValueObjectFactory<TestSingleEnumValueObject> Rehydrate()
        {
            return (property, _) => new TestSingleEnumValueObject(property.ToEnumOrDefault(TestEnum.ADefault));
        }

        public TestEnum EnumValue => Value;
    }

    public sealed class TestSingleListValueObjectValueObject : SingleValueObjectBase<
        TestSingleListValueObjectValueObject,
        List<TestSingleStringValueObject>>
    {
        public TestSingleListValueObjectValueObject(List<TestSingleStringValueObject> value) : base(value)
        {
        }

        public static ValueObjectFactory<TestSingleListValueObjectValueObject> Rehydrate()
        {
            return (property, _) => new TestSingleListValueObjectValueObject(property.FromJson<List<string>>()!
                .Select(item => new TestSingleStringValueObject(item))
                .ToList());
        }

        public List<TestSingleStringValueObject> Values => Value;
    }

    public sealed class TestMultiValueObject : ValueObjectBase<TestMultiValueObject>
    {
        public TestMultiValueObject(string @string, int integer, bool boolean)
        {
            AStringValue = @string;
            AnIntegerValue = integer;
            ABooleanValue = boolean;
        }

        public static ValueObjectFactory<TestMultiValueObject> Rehydrate()
        {
            return (property, _) =>
            {
                var values = ValueObjectBase<TestMultiValueObject>.RehydrateToList(property, false);
                return new TestMultiValueObject(values[0], values[1].ToInt(), values[2].ToBool());
            };
        }

        protected override IEnumerable<object?> GetAtomicValues()
        {
            return new object[] { AStringValue, AnIntegerValue, ABooleanValue };
        }

        public bool ABooleanValue { get; }

        public int AnIntegerValue { get; }

        public string AStringValue { get; }

        public new static List<string> RehydrateToList(string hydratedValue, bool isSingleValueObject,
            bool isSingleListValueObject = false)
        {
            return ValueObjectBase<TestMultiValueObject>.RehydrateToList(hydratedValue, isSingleValueObject,
                isSingleListValueObject);
        }
    }

    public sealed class TestMultiValueObjectWithNoValues : ValueObjectBase<TestMultiValueObjectWithNoValues>
    {
        public static ValueObjectFactory<TestMultiValueObjectWithNoValues> Rehydrate()
        {
            return (_, _) => new TestMultiValueObjectWithNoValues();
        }

        protected override IEnumerable<object?> GetAtomicValues()
        {
            return Array.Empty<object>();
        }
    }

#pragma warning disable S2344
    public enum TestEnum
#pragma warning restore S2344
    {
        ADefault = 0,
        AValue1 = 1,
        AValue2 = 2
    }
}