using Common.Extensions;
using FluentAssertions;
using Xunit;

namespace Common.UnitTests;

[Trait("Category", "Unit")]
public class StringExtensionsSpec
{
    [Fact]
    public void WhenHasValueAndNull_ThenReturnsFalse()
    {
        var result = ((string?)null).HasValue();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasValueAndEmpty_ThenReturnsFalse()
    {
        var result = string.Empty.HasValue();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasValueAndOnlyWhitespace_ThenReturnsFalse()
    {
        var result = " ".HasValue();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasValueAndHasValue_ThenReturnsTrue()
    {
        var result = "avalue".HasValue();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenToJsonAndNull_ThenReturnsNull()
    {
        var result = ((string)null!).ToJson();

        result.Should().BeNull();
    }

    [Fact]
    public void WhenToJsonAndDefaults_ThenReturnsJsonPrettyPrintedWithNoNullsInPascal()
    {
        var result = new
        {
            Property1 = "avalue",
            Property2 = (string)null!,
            Property3 = ""
        }.ToJson();

        result.Should().Be("""
                           {
                             "Property1": "avalue",
                             "Property3": ""
                           }
                           """);
    }

    [Fact]
    public void WhenToJsonAndCamelCase_ThenReturnsJsonPrettyPrintedWithNoNullsInCamel()
    {
        var result = new
        {
            Property1 = "avalue",
            Property2 = (string)null!,
            Property3 = ""
        }.ToJson(casing: StringExtensions.JsonCasing.Camel);

        result.Should().Be("""
                           {
                             "property1": "avalue",
                             "property3": ""
                           }
                           """);
    }

    [Fact]
    public void WhenToJsonAndNotPretty_ThenReturnsJsonWithNoNullsInPascal()
    {
        var result = new
        {
            Property1 = "avalue",
            Property2 = (string)null!,
            Property3 = ""
        }.ToJson(false);

        result.Should().Be("{\"Property1\":\"avalue\",\"Property3\":\"\"}");
    }

    [Fact]
    public void WhenToJsonAndIncludeNulls_ThenReturnsJsonPrettyPrintedWithNullsInPascal()
    {
        var result = new
        {
            Property1 = "avalue",
            Property2 = (string)null!,
            Property3 = ""
        }.ToJson(includeNulls: true);

        result.Should().Be("""
                           {
                             "Property1": "avalue",
                             "Property2": null,
                             "Property3": ""
                           }
                           """);
    }
}