using Common;
using FluentAssertions;
using Xunit;

namespace Application.Interfaces.UnitTests;

[Trait("Category", "Unit")]
public class SearchOptionsSpec
{
    private readonly SearchOptions _searchOptions = new();

    [Fact]
    public void WhenApplyWithMetadataAndNoLimit_ThenTakesDefaultLimit()
    {
        _searchOptions.Limit = SearchOptions.NoLimit;
        var queried = Enumerable.Range(1, SearchOptions.MaxLimit + 1)
            .ToList();

        var result = _searchOptions.ApplyWithMetadata(queried);

        result.Results.Count.Should().Be(SearchOptions.DefaultLimit);
        result.Metadata.Filter!.Fields.Any()
            .Should().BeFalse();
        result.Metadata.Sort!.Should().BeNull();
        result.Metadata.Offset.Should().Be(SearchOptions.NoOffset);
        result.Metadata.Limit.Should().Be(SearchOptions.NoLimit);
        result.Metadata.Total.Should().Be(SearchOptions.MaxLimit + 1);
    }

    [Fact]
    public void WhenApplyWithMetadataAndLimitLessThanMax_ThenTakesLimit()
    {
        _searchOptions.Limit = SearchOptions.MaxLimit - 1;
        var queried = Enumerable.Range(1, SearchOptions.MaxLimit + 1)
            .ToList();

        var result = _searchOptions.ApplyWithMetadata(queried);

        result.Results.Count.Should().Be(SearchOptions.MaxLimit - 1);
        AssertSearchResults(result);
        result.Metadata.Total.Should().Be(SearchOptions.MaxLimit + 1);
    }

    [Fact]
    public void WhenApplyWithMetadataAndLimitGreaterThanMax_ThenTakesMaxLimit()
    {
        _searchOptions.Limit = SearchOptions.MaxLimit + 1;
        var queried = Enumerable.Range(1, SearchOptions.MaxLimit + 1)
            .ToList();

        var result = _searchOptions.ApplyWithMetadata(queried);

        result.Results.Count.Should().Be(SearchOptions.MaxLimit);
        AssertSearchResults(result);
        result.Metadata.Total.Should().Be(SearchOptions.MaxLimit + 1);
    }

    [Fact]
    public void WhenApplyWithMetadataAndLimitLessThanMaxAndQueriedLessThanLimit_ThenTakesMaxQueried()
    {
        _searchOptions.Limit = SearchOptions.MaxLimit - 1;
        var queried = Enumerable.Range(1, 66)
            .ToList();

        var result = _searchOptions.ApplyWithMetadata(queried);

        result.Results.Count.Should().Be(66);
        AssertSearchResults(result);
        result.Metadata.Total.Should().Be(66);
    }

    [Fact]
    public void WhenApplyWithMetadataAndLimitGreaterThanMaxAndQueriedLessThanLimit_ThenTakesMaxQueried()
    {
        _searchOptions.Limit = SearchOptions.MaxLimit + 1;
        var queried = Enumerable.Range(1, 66)
            .ToList();

        var result = _searchOptions.ApplyWithMetadata(queried);

        result.Results.Count.Should().Be(66);
        AssertSearchResults(result);
        result.Metadata.Total.Should().Be(66);
    }

    [Fact]
    public void WhenApplyWithMetadataAndNoSorting_ThenNoOrdering()
    {
        _searchOptions.Sort = new Optional<Sorting>();
        var queried = new[]
        {
            1,
            6,
            3
        }.ToList();

        var result = _searchOptions.ApplyWithMetadata(queried);

        var results = result.Results.ToList();
        results.Count.Should().Be(3);
        results[0]
            .Should().Be(1);
        results[1]
            .Should().Be(6);
        results[2]
            .Should().Be(3);
        AssertSearchResults(result);
        result.Metadata.Total.Should().Be(3);
    }

    [Fact]
    public void WhenApplyWithMetadataAndSortByIsEmpty_ThenNoOrdering()
    {
        _searchOptions.Sort = new Optional<Sorting>();
        var queried = new[]
        {
            1,
            6,
            3
        }.ToList();

        var result = _searchOptions.ApplyWithMetadata(queried);

        var results = result.Results.ToList();
        results.Count.Should().Be(3);
        results[0]
            .Should().Be(1);
        results[1]
            .Should().Be(6);
        results[2]
            .Should().Be(3);
        AssertSearchResults(result);
        result.Metadata.Total.Should().Be(3);
    }

    [Fact]
    public void WhenApplyWithMetadataAndSortByIsUnknown_ThenNoOrdering()
    {
        _searchOptions.Sort = new Sorting("unknown");
        var queried = new[]
        {
            1,
            6,
            3
        }.ToList();

        var result = _searchOptions.ApplyWithMetadata(queried);

        var results = result.Results.ToList();
        results.Count.Should().Be(3);
        results[0]
            .Should().Be(1);
        results[1]
            .Should().Be(6);
        results[2]
            .Should().Be(3);
        AssertSearchResults(result);
        result.Metadata.Total.Should().Be(3);
    }

    [Fact]
    public void WhenApplyWithMetadataAndSortDirectionDescending_ThenOrderingByDefault()
    {
        _searchOptions.Sort = new Sorting("AProperty");
        var queried = new[]
        {
            new Sortable { AProperty = 1 },
            new Sortable { AProperty = 6 },
            new Sortable { AProperty = 3 }
        }.ToList();

        var result = _searchOptions.ApplyWithMetadata(queried);

        var results = result.Results.ToList();
        results.Count.Should().Be(3);
        results[0]
            .AProperty.Should().Be(1);
        results[1]
            .AProperty.Should().Be(3);
        results[2]
            .AProperty.Should().Be(6);
        AssertSearchResults(result);
        result.Metadata.Total.Should().Be(3);
    }

    [Fact]
    public void WhenApplyWithMetadataAndSortDirectionAscending_ThenOrderingAscending()
    {
        _searchOptions.Sort = new Sorting("AProperty");
        var queried = new[]
        {
            new Sortable { AProperty = 1 },
            new Sortable { AProperty = 6 },
            new Sortable { AProperty = 3 }
        }.ToList();

        var result = _searchOptions.ApplyWithMetadata(queried);

        var results = result.Results.ToList();
        results.Count.Should().Be(3);
        results[0]
            .AProperty.Should().Be(1);
        results[1]
            .AProperty.Should().Be(3);
        results[2]
            .AProperty.Should().Be(6);
        AssertSearchResults(result);
        result.Metadata.Total.Should().Be(3);
    }

    [Fact]
    public void WhenApplyWithMetadataAndSortDirectionDescending_ThenOrderingDescending()
    {
        _searchOptions.Sort = new Sorting("AProperty", SortDirection.Descending);
        var queried = new[]
        {
            new Sortable { AProperty = 1 },
            new Sortable { AProperty = 6 },
            new Sortable { AProperty = 3 }
        }.ToList();

        var result = _searchOptions.ApplyWithMetadata(queried);

        var results = result.Results.ToList();
        results.Count.Should().Be(3);
        results[0]
            .AProperty.Should().Be(6);
        results[1]
            .AProperty.Should().Be(3);
        results[2]
            .AProperty.Should().Be(1);
        AssertSearchResults(result);
        result.Metadata.Total.Should().Be(3);
    }

    private void AssertSearchResults<T>(SearchResults<T> result)
    {
        result.Metadata.Filter!.Fields.Should().BeSameAs(_searchOptions.Filter.Fields);
        if (_searchOptions.Sort.HasValue)
        {
            result.Metadata.Sort!.By.Should()
                .Be(_searchOptions.Sort.Value.By);
            result.Metadata.Sort.Direction.Should()
                .Be(_searchOptions.Sort.Value.Direction);
        }
        else
        {
            result.Metadata.Sort.Should()
                .BeNull();
        }

        result.Metadata.Offset.Should().Be(_searchOptions.Offset);
        result.Metadata.Limit.Should().Be(_searchOptions.Limit);
    }
}

public class Sortable
{
    public int AProperty { get; set; }
}