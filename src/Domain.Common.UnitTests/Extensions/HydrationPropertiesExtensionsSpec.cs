using Common;
using Domain.Common.Extensions;
using Domain.Interfaces;
using UnitTesting.Common;
using Xunit;

namespace Domain.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class HydrationPropertiesExtensionsSpec
{
    [Fact]
    public void WhenGetValueOrDefaultForUnknownProperty_ThenReturnsNone()
    {
        var result = new HydrationProperties()
            .GetValueOrDefault<string>("aname");

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetValueOrDefaultForReferenceTypeAndNullWithNoDefault_ThenReturnsNone()
    {
        var result = new HydrationProperties
            {
                { "aname", (string?)null }
            }
            .GetValueOrDefault<string>("aname");

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetValueOrDefaultForOptionalReferenceTypeAndNullWithNoDefault_ThenReturnsNone()
    {
        var result = new HydrationProperties
            {
                { "aname", (Optional<string>)null }
            }
            .GetValueOrDefault<string>("aname");

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetValueOrDefaultForOptionalReferenceTypeAndValueWithNoDefault_ThenReturnsNone()
    {
        var result = new HydrationProperties
            {
                { "aname", (Optional<string>)"avalue" }
            }
            .GetValueOrDefault<string>("aname");

        result.Should().BeSome("avalue");
    }

    [Fact]
    public void WhenGetValueOrDefaultForValueTypeAndNullWithNoDefault_ThenReturnsNone()
    {
        var result = new HydrationProperties
            {
                { "aname", (DateTime?)null }
            }
            .GetValueOrDefault<DateTime>("aname");

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetValueOrDefaultForOptionalValueTypeAndNullWithNoDefault_ThenReturnsNone()
    {
        var result = new HydrationProperties
            {
                { "aname", (Optional<DateTime?>)null }
            }
            .GetValueOrDefault<DateTime>("aname");

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetValueOrDefaultForOptionalValueTypeAndValueWithNoDefault_ThenReturnsNone()
    {
        var datum = DateTime.UtcNow;
        var result = new HydrationProperties
            {
                { "aname", (Optional<DateTime?>)datum }
            }
            .GetValueOrDefault<DateTime>("aname");

        result.Should().BeSome(datum);
    }

    [Fact]
    public void WhenGetValueOrDefaultAndNullWithDefault_ThenReturnsDefault()
    {
        var result = new HydrationProperties
            {
                { "aname", (string?)null }
            }
            .GetValueOrDefault<string>("aname", "adefaultvalue");

        result.Should().BeSome("adefaultvalue");
    }

    [Fact]
    public void WhenGetValueOrDefaultAndNullWithNullDefault_ThenReturnsNone()
    {
        var result = new HydrationProperties
            {
                { "aname", (string?)null }
            }
            .GetValueOrDefault<string>("aname", null!);

        result.Should().BeNone();
    }

    [Fact]
    public void WhenGetValueOrDefaultAndHasValue_ThenReturnsValue()
    {
        var result = new HydrationProperties
            {
                { "aname", "avalue" }
            }
            .GetValueOrDefault<string>("aname", "adefaultvalue");

        result.Should().BeSome("avalue");
    }
}