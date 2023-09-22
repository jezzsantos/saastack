using FluentAssertions;
using Xunit;

namespace Application.Interfaces.UnitTests;

[Trait("Category", "Unit")]
public class SearchOptionsExtensionsSpec
{
    [Fact]
    public void WhenToMetadataSafeWithNullSearchOptions_ThenReturnsDefaultSearchMetadata()
    {
        var results = ((SearchOptions)null!).ToMetadata();

        results.Total.Should().Be(0);
    }

    [Fact]
    public void WhenToMetadataSafeWithNullSearchOptionsAndTotal_ThenReturnsDefaultSearchMetadata()
    {
        var results = ((SearchOptions)null!).ToMetadata(11);

        results.Total.Should().Be(11);
    }

    [Fact]
    public void WhenToMetadataSafeWithInitialSearchOptions_ThenReturnsSearchMetadata()
    {
        var searchOptions = new SearchOptions();

        var results = searchOptions.ToMetadata();

        results.Total.Should().Be(0);
    }

    [Fact]
    public void WhenToMetadataSafe_ThenReturnsPopulatedSearchMetadata()
    {
        var searchOptions = new SearchOptions
        {
            Sort = new Sorting("asortfield", SortDirection.Descending),
            Offset = 9,
            Limit = 6,
            Filter = new Filtering("afilterfield")
        };

        var results = searchOptions.ToMetadata();

        results.Total.Should().Be(0);
        results.Sort!.Direction.Should().Be(SortDirection.Descending);
        results.Sort.By.Should().Be("asortfield");
        results.Offset.Should().Be(9);
        results.Limit.Should().Be(6);
        results.Filter!.Fields.Count.Should().Be(1);
        results.Filter.Fields[0].Should().Be("afilterfield");
    }

    [Fact]
    public void WhenToMetadataSafeAndTotal_ThenReturnsPopulatedSearchMetadata()
    {
        var searchOptions = new SearchOptions
        {
            Sort = new Sorting("asortfield", SortDirection.Descending),
            Offset = 9,
            Limit = 6,
            Filter = new Filtering("afilterfield")
        };

        var results = searchOptions.ToMetadata(11);

        results.Total.Should().Be(11);
        results.Sort!.Direction.Should().Be(SortDirection.Descending);
        results.Sort.By.Should().Be("asortfield");
        results.Offset.Should().Be(9);
        results.Limit.Should().Be(6);
        results.Filter!.Fields.Count.Should().Be(1);
        results.Filter.Fields[0].Should().Be("afilterfield");
    }
}