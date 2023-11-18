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