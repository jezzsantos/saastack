using Common.Extensions;
using FluentAssertions;
using JetBrains.Annotations;
using UnitTesting.Common.Validation;
using Xunit;

namespace Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class DictionaryExtensionsSpec
{
    [Fact]
    public void WhenMergeAndSourceAndOtherIsEmpty_ThenContainsNothing()
    {
        var source = new Dictionary<string, string>();

        source.Merge(new Dictionary<string, string>());

        source.Count.Should().Be(0);
    }

    [Fact]
    public void WhenMergeAndOtherIsEmpty_ThenNothingAdded()
    {
        var source = new Dictionary<string, string>
        {
            { "aname", "avalue" }
        };

        source.Merge(new Dictionary<string, string>());

        source.Count.Should().Be(1);
        source.Should().OnlyContain(pair => pair.Key == "aname");
    }

    [Fact]
    public void WhenMergeAndSourceIsEmpty_ThenOtherAdded()
    {
        var source = new Dictionary<string, string>();

        source.Merge(new Dictionary<string, string>
        {
            { "aname", "avalue" }
        });

        source.Count.Should().Be(1);
        source.Should().OnlyContain(pair => pair.Key == "aname");
    }

    [Fact]
    public void WhenMergeAndSourceAndOtherHaveUniqueKeys_ThenOtherAdded()
    {
        var source = new Dictionary<string, string>
        {
            { "aname1", "avalue1" }
        };

        source.Merge(new Dictionary<string, string>
        {
            { "aname2", "avalue2" }
        });

        source.Count.Should().Be(2);
        source.Should().Contain(pair => pair.Key == "aname1");
        source.Should().Contain(pair => pair.Key == "aname2");
    }

    [Fact]
    public void WhenMergeAndSourceAndOtherHaveSameKeys_ThenSourceRemains()
    {
        var source = new Dictionary<string, string>
        {
            { "aname1", "avalue1" },
            { "aname2", "avalue2" }
        };

        source.Merge(new Dictionary<string, string>
        {
            { "aname2", "avalue2" },
            { "aname3", "avalue3" }
        });

        source.Count.Should().Be(3);
        source.Should().Contain(pair => pair.Key == "aname1");
        source.Should().Contain(pair => pair.Key == "aname2");
        source.Should().Contain(pair => pair.Key == "aname3");
    }

    [Fact]
    public void WhenFromObjectDictionaryWithEmptyInstance_ThenReturnsDefaultInstance()
    {
        var result = new Dictionary<string, object?>().AsReadOnly()
            .FromObjectDictionary<TestMappingClass>();

        result.AStringProperty.Should().Be("adefaultvalue");
        result.AnIntProperty.Should().Be(1);
        result.AnBoolProperty.Should().Be(true);
        result.ADateTimeProperty.Should().BeNear(DateTime.UtcNow);
        result.AnOptionalStringProperty.Should().Be(Optional<string>.None);
        result.AnOptionalNullableStringProperty.Should().Be(Optional<string?>.None);
        result.AnOptionalDateTimeProperty.Should().Be(Optional<DateTime>.None);
        result.AnOptionalNullableDateTimeProperty.Should().Be(Optional<DateTime?>.None);
    }

    [Fact]
    public void WhenFromObjectDictionaryWithNonMatchingProperties_ThenReturnsDefaultInstance()
    {
        var result = new Dictionary<string, object?>
            {
                { "anunknownproperty", "avalue" }
            }.AsReadOnly()
            .FromObjectDictionary<TestMappingClass>();

        result.AStringProperty.Should().Be("adefaultvalue");
        result.AnIntProperty.Should().Be(1);
        result.AnBoolProperty.Should().Be(true);
        result.ADateTimeProperty.Should().BeNear(DateTime.UtcNow);
        result.AnOptionalStringProperty.Should().Be(Optional<string>.None);
        result.AnOptionalNullableStringProperty.Should().Be(Optional<string?>.None);
        result.AnOptionalDateTimeProperty.Should().Be(Optional<DateTime>.None);
        result.AnOptionalNullableDateTimeProperty.Should().Be(Optional<DateTime?>.None);
    }

    [Fact]
    public void WhenFromObjectDictionaryWithMatchingProperties_ThenReturnsUpdatedInstance()
    {
        var datum = DateTime.Today;
        var result = new Dictionary<string, object?>
            {
                { nameof(TestMappingClass.AStringProperty), "avalue" },
                { nameof(TestMappingClass.AnIntProperty), 99 },
                { nameof(TestMappingClass.AnBoolProperty), false },
                { nameof(TestMappingClass.ADateTimeProperty), datum },
                { nameof(TestMappingClass.AnOptionalStringProperty), "avalue" },
                { nameof(TestMappingClass.AnOptionalNullableStringProperty), "avalue" },
                { nameof(TestMappingClass.AnOptionalDateTimeProperty), datum },
                { nameof(TestMappingClass.AnOptionalNullableDateTimeProperty), datum }
            }.AsReadOnly()
            .FromObjectDictionary<TestMappingClass>();

        result.AStringProperty.Should().Be("avalue");
        result.AnIntProperty.Should().Be(99);
        result.AnBoolProperty.Should().Be(false);
        result.ADateTimeProperty.Should().Be(datum);
        result.AnOptionalStringProperty.Should().Be("avalue");
        result.AnOptionalNullableStringProperty.Should().Be("avalue");
        result.AnOptionalDateTimeProperty.Should().Be(datum);
        result.AnOptionalNullableDateTimeProperty.Should().Be(datum);
    }

    [Fact]
    public void WhenFromObjectDictionaryWithMatchingNullProperties_ThenReturnsUpdatedInstanceWithDefaultValues()
    {
        var result = new Dictionary<string, object?>
            {
                { nameof(TestMappingClass.AStringProperty), null },
                { nameof(TestMappingClass.AnIntProperty), null },
                { nameof(TestMappingClass.AnBoolProperty), null },
                { nameof(TestMappingClass.ADateTimeProperty), null },
                { nameof(TestMappingClass.AnOptionalStringProperty), null },
                { nameof(TestMappingClass.AnOptionalNullableStringProperty), null },
                { nameof(TestMappingClass.AnOptionalDateTimeProperty), null },
                { nameof(TestMappingClass.AnOptionalNullableDateTimeProperty), null }
            }.AsReadOnly()
            .FromObjectDictionary<TestMappingClass>();

        result.AStringProperty.Should().BeNull();
        result.AnIntProperty.Should().Be(default);
        result.AnBoolProperty.Should().Be(default);
        result.ADateTimeProperty.Should().Be(default);
        result.AnOptionalStringProperty.Should().Be(Optional<string>.None);
        result.AnOptionalNullableStringProperty.Should().Be(Optional<string?>.None);
        result.AnOptionalDateTimeProperty.Should().Be(Optional<DateTime>.None);
        result.AnOptionalNullableDateTimeProperty.Should().Be(Optional<DateTime?>.None);
    }

    [Fact]
    public void WhenToObjectDictionaryWithNullInstance_ThenReturnsEmpty()
    {
        var result = ((TestMappingClass?)null).ToObjectDictionary();

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenToObjectDictionaryWithInstanceWithValues_ThenReturnsProperties()
    {
        var result = new TestMappingClass().ToObjectDictionary();

        result.Count.Should().Be(8);
        result[nameof(TestMappingClass.AStringProperty)].Should().Be("adefaultvalue");
        result[nameof(TestMappingClass.AnIntProperty)].Should().Be(1);
        result[nameof(TestMappingClass.AnBoolProperty)].Should().Be(true);
        result[nameof(TestMappingClass.ADateTimeProperty)].As<DateTime>().Should().BeNear(DateTime.UtcNow);
        result[nameof(TestMappingClass.AnOptionalStringProperty)].Should().Be(Optional<string>.None);
        result[nameof(TestMappingClass.AnOptionalNullableStringProperty)].Should().Be(Optional<string?>.None);
        result[nameof(TestMappingClass.AnOptionalDateTimeProperty)].Should().Be(Optional<DateTime>.None);
        result[nameof(TestMappingClass.AnOptionalNullableDateTimeProperty)].Should().Be(Optional<DateTime?>.None);
    }

    [Fact]
    public void WhenToObjectDictionaryWithInstanceWithDefaultValues_ThenReturnsProperties()
    {
        var result = new TestMappingClass
        {
            AStringProperty = default!,
            AnIntProperty = default,
            AnBoolProperty = default,
            ADateTimeProperty = default,
            AnOptionalStringProperty = default,
            AnOptionalNullableStringProperty = default,
            AnOptionalDateTimeProperty = default,
            AnOptionalNullableDateTimeProperty = default
        }.ToObjectDictionary();

        result.Count.Should().Be(8);
        result[nameof(TestMappingClass.AStringProperty)].Should().BeNull();
        result[nameof(TestMappingClass.AnIntProperty)].Should().Be(default(int));
        result[nameof(TestMappingClass.AnBoolProperty)].Should().Be(default(bool));
        result[nameof(TestMappingClass.ADateTimeProperty)].Should().Be(default(DateTime));
        result[nameof(TestMappingClass.AnOptionalStringProperty)].Should().Be(Optional<string>.None);
        result[nameof(TestMappingClass.AnOptionalNullableStringProperty)].Should().Be(Optional<string?>.None);
        result[nameof(TestMappingClass.AnOptionalDateTimeProperty)].Should().Be(Optional<DateTime>.None);
        result[nameof(TestMappingClass.AnOptionalNullableDateTimeProperty)].Should().Be(Optional<DateTime?>.None);
    }

    [Fact]
    public void WhenToStringDictionaryWithNullInstance_ThenreturnsEmpty()
    {
        var result = ((TestMappingClass?)null).ToStringDictionary();

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenToStringDictionaryWithInstanceWithValues_ThenReturnsProperties()
    {
        var datum = DateTime.UtcNow;
        var result = new TestMappingClass
        {
            ADateTimeProperty = datum,
            AnOptionalStringProperty = "avalue",
            AnOptionalNullableStringProperty = "avalue",
            AnOptionalDateTimeProperty = datum,
            AnOptionalNullableDateTimeProperty = datum
        }.ToStringDictionary();

        result.Count.Should().Be(8);
        result[nameof(TestMappingClass.AStringProperty)].Should().Be("adefaultvalue");
        result[nameof(TestMappingClass.AnIntProperty)].Should().Be("1");
        result[nameof(TestMappingClass.AnBoolProperty)].Should().Be("True");
        result[nameof(TestMappingClass.ADateTimeProperty)].Should().Be(datum.ToIso8601());
        result[nameof(TestMappingClass.AnOptionalStringProperty)].Should().Be("avalue");
        result[nameof(TestMappingClass.AnOptionalNullableStringProperty)].Should().Be("avalue");
        result[nameof(TestMappingClass.AnOptionalDateTimeProperty)].Should().Be(datum.ToIso8601());
        result[nameof(TestMappingClass.AnOptionalNullableDateTimeProperty)].Should().Be(datum.ToIso8601());
    }

    [Fact]
    public void WhenToStringDictionaryWithInstanceWithDefaultValues_ThenReturnsProperties()
    {
        var result = new TestMappingClass
        {
            AStringProperty = default!,
            AnIntProperty = default,
            AnBoolProperty = default,
            ADateTimeProperty = default,
            AnOptionalStringProperty = default,
            AnOptionalNullableStringProperty = default,
            AnOptionalDateTimeProperty = default,
            AnOptionalNullableDateTimeProperty = default
        }.ToStringDictionary();

        result.Count.Should().Be(3);
        result[nameof(TestMappingClass.AnIntProperty)].Should().Be("0");
        result[nameof(TestMappingClass.AnBoolProperty)].Should().Be("False");
        result[nameof(TestMappingClass.ADateTimeProperty)].Should().Be("0001-01-01T00:00:00");
    }
}

[UsedImplicitly]
public class TestMappingClass
{
    public DateTime ADateTimeProperty { get; set; } = DateTime.UtcNow;

    public bool AnBoolProperty { get; set; } = true;

    public int AnIntProperty { get; set; } = 1;

    public Optional<DateTime> AnOptionalDateTimeProperty { get; set; }

    public Optional<DateTime?> AnOptionalNullableDateTimeProperty { get; set; }

    public Optional<string?> AnOptionalNullableStringProperty { get; set; }

    public Optional<string> AnOptionalStringProperty { get; set; }

    public string AStringProperty { get; set; } = "adefaultvalue";
}