using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using FluentAssertions;
using UnitTesting.Common;
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
    public void WhenDehydrateMultiPropertyValueWithDefaults_ThenReturnsProperties()
    {
        var result = new TestMultiValueObject(null!, 0, false, DateTime.MinValue, Optional<string>.None,
            Optional<int>.None, Optional<bool>.None, Optional<DateTime>.None).Dehydrate();

        result.Should()
            .Be(
                "{\"Val1\":\"NULL\",\"Val2\":0,\"Val3\":false,\"Val4\":\"0001-01-01T00:00:00\",\"Val5\":\"NULL\",\"Val6\":\"NULL\",\"Val7\":\"NULL\",\"Val8\":\"NULL\"}");
    }

    [Fact]
    public void WhenDehydrateMultiPropertyValueWithValues_ThenReturnsProperties()
    {
        var datum1 = DateTime.UtcNow.AddSeconds(2);
        var datum2 = DateTime.UtcNow.SubtractSeconds(2);

        var result = new TestMultiValueObject("avalue1", 25, true, datum1, new Optional<string>("avalue2"),
            new Optional<int>(75), new Optional<bool>(true), new Optional<DateTime>(datum2)).Dehydrate();

        result.Should()
            .Be(
                $"{{\"Val1\":\"avalue1\",\"Val2\":25,\"Val3\":true,\"Val4\":\"{datum1.ToIso8601()}\",\"Val5\":\"avalue2\",\"Val6\":75,\"Val7\":true,\"Val8\":\"{datum2.ToIso8601()}\"}}");
    }

    [Fact]
    public void WhenDehydrateSingleListStringValues_ThenReturnsProperties()
    {
        var value = new List<string>
        {
            "avalue1",
            "avalue2",
            null!
        };

        var result = new TestSingleListStringValueObject(value).Dehydrate();

        result.Should().Be("[\"avalue1\",\"avalue2\",null]");
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
    public void WhenRehydrateMultiValueWithDefaultValues_ThenReturnsInstance()
    {
        var valueObject = TestMultiValueObject.Rehydrate()(
            "{\"Val1\":\"NULL\",\"Val2\":25,\"Val3\":true,\"Val4\":\"0001-01-01T00:00:00\",\"Val5\":\"NULL\",\"Val6\":\"NULL\",\"Val7\":\"NULL\",\"Val8\":\"NULL\"}",
            null!);

        valueObject.AStringValue.Should().BeNull();
        valueObject.AnIntegerValue.Should().Be(25);
        valueObject.ABooleanValue.Should().BeTrue();
        valueObject.ADateTimeValue.Should().Be(DateTime.MinValue);
        valueObject.AnOptionalStringValue.Should().BeNone();
        valueObject.AnOptionalIntegerValue.Should().BeNone();
        valueObject.AnOptionalBooleanValue.Should().BeNone();
        valueObject.AnOptionalDateTimeValue.Should().BeNone();
    }

    [Fact]
    public void WhenRehydrateMultiValue_ThenReturnsInstance()
    {
        var datum1 = DateTime.UtcNow.AddSeconds(2);
        var datum2 = DateTime.UtcNow.SubtractSeconds(2);

        var valueObject = TestMultiValueObject.Rehydrate()(
            $"{{\"Val1\":\"avalue1\",\"Val2\":25,\"Val3\":true,\"Val4\":\"{datum1.ToIso8601()}\",\"Val5\":\"avalue2\",\"Val6\":75,\"Val7\":true,\"Val8\":\"{datum2.ToIso8601()}\"}}",
            null!);

        valueObject.AStringValue.Should().Be("avalue1");
        valueObject.AnIntegerValue.Should().Be(25);
        valueObject.ABooleanValue.Should().BeTrue();
        valueObject.ADateTimeValue.Should().Be(datum1);
        valueObject.AnOptionalStringValue.Should().BeSome("avalue2");
        valueObject.AnOptionalIntegerValue.Should().BeSome(75);
        valueObject.AnOptionalBooleanValue.Should().BeSome(true);
        valueObject.AnOptionalDateTimeValue.Should().BeSome(datum2);
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
    public void WhenRehydrateToListWithDefaultValues_ThenReturnsValues()
    {
        var datum1 = DateTime.UtcNow.AddSeconds(2);

        var result = TestMultiValueObject.RehydrateToList(
            $"{{\"Val1\":\"NULL\",\"Val2\":25,\"Val3\":true,\"Val4\":\"{datum1.ToIso8601()}\",\"Val5\":\"NULL\",\"Val6\":\"NULL\",\"Val7\":\"NULL\",\"Val8\":\"NULL\"}}",
            false);

        result.Should().HaveCount(8);
        result[0].Should().Be(Optional<string>.None);
        result[1].Should().Be(new Optional<string>("25"));
        result[2].Should().Be(new Optional<string>("True"));
        result[3].Should().Be(new Optional<string>(datum1.ToIso8601()));
        result[4].Should().Be(Optional<string>.None);
        result[5].Should().Be(Optional<string>.None);
        result[6].Should().Be(Optional<string>.None);
        result[7].Should().Be(Optional<string>.None);
    }

    [Fact]
    public void WhenRehydrateToListWithAnEmptyValue_ThenReturnsValues()
    {
        var datum1 = DateTime.UtcNow.AddSeconds(2);
        var datum2 = DateTime.UtcNow.SubtractSeconds(2);

        var result = TestMultiValueObject.RehydrateToList(
            $"{{\"Val1\":\"avalue1\",\"Val2\":25,\"Val3\":true,\"Val4\":\"{datum1.ToIso8601()}\",\"Val5\":\"avalue2\",\"Val6\":75,\"Val7\":true,\"Val8\":\"{datum2.ToIso8601()}\"}}",
            false);

        result.Should().HaveCount(8);
        result[0].Should().Be(new Optional<string>("avalue1"));
        result[1].Should().Be(new Optional<string>("25"));
        result[2].Should().Be(new Optional<string>("True"));
        result[3].Should().Be(new Optional<string>(datum1.ToIso8601()));
        result[4].Should().Be(new Optional<string>("avalue2"));
        result[5].Should().Be(new Optional<string>("75"));
        result[6].Should().Be(new Optional<string>("True"));
        result[7].Should().Be(new Optional<string>(datum2.ToIso8601()));
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
        var result = TestMultiValueObject.RehydrateToList(string.Empty, true);

        result.Should().ContainInOrder(string.Empty);
    }

    [Fact]
    public void WhenRehydrateToListWithSpecialNullValueForSingleValue_ThenReturnsEmpty()
    {
        var result = TestMultiValueObject.RehydrateToList(ValueObjectBase<TestMultiValueObject>.NullValue, true);

        result.Should().ContainInOrder((string?)null);
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
        var result = TestMultiValueObject.RehydrateToList("[\"astringvalue\",\"25\",\"true\",\"0001-01-01T00:00:00\"]",
            true, true);

        result.Should().ContainInOrder("astringvalue", "25", "true", "0001-01-01T00:00:00");
    }

    public sealed class TestSingleListStringValueObject : SingleValueObjectBase<TestSingleListStringValueObject,
        List<string>>
    {
        public TestSingleListStringValueObject(List<string> value) : base(value)
        {
        }

        public List<string> Values => Value;

        public static ValueObjectFactory<TestSingleListStringValueObject> Rehydrate()
        {
            return (property, _) => new TestSingleListStringValueObject(property.FromJson<List<string>>()!);
        }
    }

    public sealed class TestSingleStringValueObject : SingleValueObjectBase<TestSingleStringValueObject, string>
    {
        public TestSingleStringValueObject(string value) : base(value)
        {
        }

        public string StringValue => Value;

        public static ValueObjectFactory<TestSingleStringValueObject> Rehydrate()
        {
            return (property, _) => new TestSingleStringValueObject(property);
        }
    }

    public sealed class TestSingleIntValueObject : SingleValueObjectBase<TestSingleIntValueObject, int>
    {
        public TestSingleIntValueObject(int value) : base(value)
        {
        }

        public int IntValue => Value;

        public static ValueObjectFactory<TestSingleStringValueObject> Rehydrate()
        {
            return (property, _) => new TestSingleStringValueObject(property);
        }
    }

    public sealed class TestSingleEnumValueObject : SingleValueObjectBase<TestSingleEnumValueObject, TestEnum>
    {
        public TestSingleEnumValueObject(TestEnum value) : base(value)
        {
        }

        public TestEnum EnumValue => Value;

        public static ValueObjectFactory<TestSingleEnumValueObject> Rehydrate()
        {
            return (property, _) => new TestSingleEnumValueObject(property.ToEnumOrDefault(TestEnum.ADefault));
        }
    }

    public sealed class TestSingleListValueObjectValueObject : SingleValueObjectBase<
        TestSingleListValueObjectValueObject,
        List<TestSingleStringValueObject>>
    {
        public TestSingleListValueObjectValueObject(List<TestSingleStringValueObject> value) : base(value)
        {
        }

        public List<TestSingleStringValueObject> Values => Value;

        public static ValueObjectFactory<TestSingleListValueObjectValueObject> Rehydrate()
        {
            return (property, _) => new TestSingleListValueObjectValueObject(property.FromJson<List<string>>()!
                .Select(item => new TestSingleStringValueObject(item))
                .ToList());
        }
    }

    public sealed class TestMultiValueObject : ValueObjectBase<TestMultiValueObject>
    {
        public TestMultiValueObject(string @string, int integer, bool boolean, DateTime dateTime)
        {
            AStringValue = @string;
            AnIntegerValue = integer;
            ABooleanValue = boolean;
            ADateTimeValue = dateTime;
            AnOptionalStringValue = Optional<string>.None;
            AnOptionalIntegerValue = Optional<int>.None;
            AnOptionalBooleanValue = Optional<bool>.None;
            AnOptionalDateTimeValue = Optional<DateTime>.None;
        }

        public TestMultiValueObject(string @string, int integer, bool boolean, DateTime dateTime,
            Optional<string> optionalString, Optional<int> optionalInteger, Optional<bool> optionalBoolean,
            Optional<DateTime> optionalDateTime)
        {
            AStringValue = @string;
            AnIntegerValue = integer;
            ABooleanValue = boolean;
            ADateTimeValue = dateTime;
            AnOptionalStringValue = optionalString;
            AnOptionalIntegerValue = optionalInteger;
            AnOptionalBooleanValue = optionalBoolean;
            AnOptionalDateTimeValue = optionalDateTime;
        }

        public bool ABooleanValue { get; }

        public DateTime ADateTimeValue { get; }

        public int AnIntegerValue { get; }

        public Optional<bool> AnOptionalBooleanValue { get; }

        public Optional<DateTime> AnOptionalDateTimeValue { get; }

        public Optional<int> AnOptionalIntegerValue { get; }

        public Optional<string> AnOptionalStringValue { get; }

        public string AStringValue { get; }

        public static ValueObjectFactory<TestMultiValueObject> Rehydrate()
        {
            return (property, _) =>
            {
                var values = ValueObjectBase<TestMultiValueObject>.RehydrateToList(property, false);
                return new TestMultiValueObject(
                    values[0],
                    values[1].Value.ToInt(),
                    values[2].Value.ToBool(),
                    values[3].Value.FromIso8601(),
                    values[4],
                    values[5].ToOptional(val => val.ToInt()),
                    values[6].ToOptional(val => val.ToBool()),
                    values[7].ToOptional(val => val.FromIso8601()));
            };
        }

        protected override IEnumerable<object?> GetAtomicValues()
        {
            return
            [
                AStringValue, AnIntegerValue, ABooleanValue, ADateTimeValue, AnOptionalStringValue,
                AnOptionalIntegerValue, AnOptionalBooleanValue, AnOptionalDateTimeValue
            ];
        }

        public new static List<Optional<string>> RehydrateToList(string hydratedValue, bool isSingleValueObject,
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