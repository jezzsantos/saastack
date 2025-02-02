using Application.Interfaces.Extensions;
using FluentAssertions;
using Xunit;

namespace Application.Interfaces.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class FilteringExtensionsSpec
{
    [Fact]
    public void WhenToFilterWithNone_ThenReturns()
    {
        var result = new Filtering()
            .ToFilter();

        result.Should().BeNull();
    }

    [Fact]
    public void WhenToFilterWithAField_ThenReturns()
    {
        var result = new Filtering("afield")
            .ToFilter();

        result.Should().Be("afield");
    }

    [Fact]
    public void WhenToFilterWithFields_ThenReturns()
    {
        var result = new Filtering(["afield1", "afield2"])
            .ToFilter();

        result.Should().Be("afield1,afield2");
    }
}