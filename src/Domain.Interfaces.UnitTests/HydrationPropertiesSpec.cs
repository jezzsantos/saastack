using Common;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Interfaces.UnitTests;

[Trait("Category", "Unit")]
public class HydrationPropertiesSpec
{
    [Fact]
    public void WhenConstructed_ThenEmpty()
    {
        var result = new HydrationProperties();

        result.Count.Should().Be(0);
        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenConstructedWithDictionaryInitializer_ThenHasValues()
    {
        var datum = DateTime.UtcNow;

        var result = new HydrationProperties
        {
            { "aname1", new Optional<string>("avalue1") },
            { "aname2", new Optional<int>(1) },
            { "aname3", new Optional<bool>(true) },
            { "aname4", new Optional<DateTime>(datum) }
        };

        result.Count.Should().Be(4);
        result["aname1"].Should().Be("avalue1");
        result["aname2"].Should().Be(1);
        result["aname3"].Should().Be(true);
        result["aname4"].Should().Be(datum);
    }

    [Fact]
    public void WhenConstructedWithDictionaryOfOptionals_ThenHasValues()
    {
        var datum = DateTime.UtcNow;
        var dictionary = new Dictionary<string, Optional<object>>
        {
            { "aname1", new Optional<string>("avalue1") },
            { "aname2", new Optional<int>(1) },
            { "aname3", new Optional<bool>(true) },
            { "aname4", new Optional<DateTime>(datum) }
        };
        var result = new HydrationProperties(dictionary);

        result.Count.Should().Be(4);
        result["aname1"].Should().Be("avalue1");
        result["aname2"].Should().Be(1);
        result["aname3"].Should().Be(true);
        result["aname4"].Should().Be(datum);
    }

    [Fact]
    public void WhenConstructedWithReadOnlyDictionaryOfValues_ThenHasValues()
    {
        var datum = DateTime.UtcNow;

        var dictionary = new Dictionary<string, object?>
        {
            { "aname1", "avalue1" },
            { "aname2", 1 },
            { "aname3", true },
            { "aname4", datum }
        };
        var result = new HydrationProperties(dictionary);

        result.Count.Should().Be(4);
        result["aname1"].Should().Be("avalue1");
        result["aname2"].Should().Be(1);
        result["aname3"].Should().Be(true);
        result["aname4"].Should().Be(datum);
    }

    [Fact]
    public void WhenToObjectDictionary_ThenReturnsReadOnlyDictionary()
    {
        var datum = DateTime.UtcNow;
        var properties = new HydrationProperties
        {
            { "aname1", new Optional<string>("avalue1") },
            { "aname2", new Optional<int>(1) },
            { "aname3", new Optional<bool>(true) },
            { "aname4", new Optional<DateTime>(datum) },
            { "aname5", Optional<string>.None },
            { "aname6", Optional<int>.None },
            { "aname7", Optional<bool>.None },
            { "aname8", Optional<DateTime>.None },
            { "aname9", Optional<string?>.None },
            { "aname10", Optional<int?>.None },
            { "aname11", Optional<bool?>.None },
            { "aname12", Optional<DateTime?>.None }
        };

        var result = properties.ToObjectDictionary();

        result.Count.Should().Be(12);
        result["aname1"].Should().Be("avalue1");
        result["aname2"].Should().Be(1);
        result["aname3"].Should().Be(true);
        result["aname4"].Should().Be(datum);
        result["aname5"].Should().Be(default(string));
        result["aname6"].Should().Be(default(int));
        result["aname7"].Should().Be(default(bool));
        result["aname8"].Should().Be(default(DateTime));
        result["aname9"].Should().BeNull();
        result["aname10"].Should().BeNull();
        result["aname11"].Should().BeNull();
        result["aname12"].Should().BeNull();
    }

    [Fact]
    public void WhenFromDtoWithNullInstance_ThenReturnsProperties()
    {
        var result = HydrationProperties.FromDto(null);

        result.Count.Should().Be(0);
        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenFromDto_ThenReturnsProperties()
    {
        var datum = DateTime.UtcNow;
        var valueObject = TestValueObject.Create("avalueobject").Value;
        var instance = new TestDto
        {
            Id = "anid",
            AStringValue = "avalue",
            AnIntegerValue = 1,
            ABooleanValue = true,
            ADateTimeValue = datum,
            AValueObject = valueObject
        };

        var result = HydrationProperties.FromDto(instance);

        result.Count.Should().Be(10);
        result[nameof(TestDto.Id)].As<Optional<object>>().Should().BeSome("anid");
        result[nameof(TestDto.AStringValue)].As<Optional<object>>().Should().BeSome("avalue");
        result[nameof(TestDto.AnIntegerValue)].As<Optional<object>>().Should().BeSome(1);
        result[nameof(TestDto.ABooleanValue)].As<Optional<object>>().Should().BeSome(true);
        result[nameof(TestDto.ADateTimeValue)].As<Optional<object>>().Should().BeSome(datum);
        result[nameof(TestDto.ANullableString)].As<Optional<object>>().Should().BeNone();
        result[nameof(TestDto.ANullableInteger)].As<Optional<object>>().Should().BeNone();
        result[nameof(TestDto.ANullableBoolean)].As<Optional<object>>().Should().BeNone();
        result[nameof(TestDto.ANullableDateTime)].As<Optional<object>>().Should().BeNone();
        result[nameof(TestDto.AValueObject)].As<Optional<object>>().Should().BeSome(valueObject);
    }

    [Fact]
    public void WhenToDtoWithNoProperties_ThenReturnsInstance()
    {
        var properties = new HydrationProperties();

        var result = properties.ToDto<TestDto>();

        result.Id.Should().BeNone();
        result.AStringValue.Should().Be(default);
        result.AnIntegerValue.Should().Be(default);
        result.ABooleanValue.Should().Be(default);
        result.ADateTimeValue.Should().Be(default);
        result.ANullableString.Should().BeNull();
        result.ANullableInteger.Should().BeNull();
        result.ANullableBoolean.Should().BeNull();
        result.ANullableDateTime.Should().BeNull();
        result.AValueObject.Should().BeNull();
    }

    [Fact]
    public void WhenToDtoWithProperties_ThenReturnsInstance()
    {
        var datum = DateTime.UtcNow;
        var valueObject = TestValueObject.Create("avalueobject").Value;
        var properties = new HydrationProperties
        {
            { nameof(TestDto.Id), new Optional<string>("anid") },
            { nameof(TestDto.AStringValue), new Optional<string>("avalue") },
            { nameof(TestDto.AnIntegerValue), new Optional<int>(1) },
            { nameof(TestDto.ABooleanValue), new Optional<bool>(true) },
            { nameof(TestDto.ADateTimeValue), new Optional<DateTime>(datum) },
            { nameof(TestDto.ANullableString), new Optional<string>((string?)null) },
            { nameof(TestDto.ANullableInteger), new Optional<string>((string?)null) },
            { nameof(TestDto.ANullableBoolean), new Optional<string>((string?)null) },
            { nameof(TestDto.ANullableDateTime), new Optional<string>((string?)null) },
            { nameof(TestDto.AValueObject), new Optional<TestValueObject>(valueObject) }
        };

        var result = properties.ToDto<TestDto>();

        result.Id.Should().Be("anid");
        result.AStringValue.Should().Be("avalue");
        result.AnIntegerValue.Should().Be(1);
        result.ABooleanValue.Should().Be(true);
        result.ADateTimeValue.Should().Be(datum);
        result.ANullableString.Should().BeNull();
        result.ANullableInteger.Should().BeNull();
        result.ANullableBoolean.Should().BeNull();
        result.ANullableDateTime.Should().BeNull();
        result.AValueObject.Should().Be(valueObject);
    }

    [Fact]
    public void WhenAddRawAndNotExist_ThenAdds()
    {
        var properties = new HydrationProperties
        {
            { "aname", "avalue" }
        };

        properties["aname"].Should().BeOfType<Optional<object>>();
        properties["aname"].Value.Should().Be("avalue");
    }

    [Fact]
    public void WhenAddRawAndExists_ThenUpdates()
    {
        var properties = new HydrationProperties
        {
            { "aname", "avalue1" }
        };

        properties.Add("aname", "avalue2");

        properties["aname"].Should().BeOfType<Optional<object>>();
        properties["aname"].Value.Should().Be("avalue2");
    }

    [Fact]
    public void WhenAddOptionalAndNotExist_ThenAdds()
    {
        var properties = new HydrationProperties
        {
            { "aname", new Optional<string>("avalue") }
        };

        properties["aname"].Value.Should().BeOfType<string>();
        properties["aname"].Value.Should().Be("avalue");
    }

    [Fact]
    public void WhenAddOptionalAndExists_ThenUpdates()
    {
        var properties = new HydrationProperties
        {
            { "aname", new Optional<string>("avalue1") }
        };

        properties.Add("aname", new Optional<string>("avalue2"));

        properties["aname"].Value.Should().BeOfType<string>();
        properties["aname"].Value.Should().Be("avalue2");
    }
}