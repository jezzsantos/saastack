using Application.Interfaces.Extensions;
using FluentAssertions;
using Xunit;

namespace Application.Interfaces.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class SortingExtensionsSpec
{
    [Fact]
    public void WhenToSortWithAField_ThenReturns()
    {
        var result = new Sorting("afield")
            .ToSort();

        result.Should().Be("afield");
    }

    [Fact]
    public void WhenToSortWithAFieldAscending_ThenReturns()
    {
        var result = new Sorting("afield")
            .ToSort();

        result.Should().Be("afield");
    }

    [Fact]
    public void WhenToSortWithAFieldDescending_ThenReturns()
    {
        var result = new Sorting("afield", SortDirection.Descending)
            .ToSort();

        result.Should().Be("-afield");
    }
}