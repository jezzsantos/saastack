using Application.Interfaces;
using FluentAssertions;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class HasSearchOptionsExtensionsSpec
{
    private readonly SearchOptionsDto _hasSearchOptions;

    public HasSearchOptionsExtensionsSpec()
    {
        _hasSearchOptions = new SearchOptionsDto();
    }

    [Fact]
    public void WhenToSearchOptionsAndNullOptions_ThenReturnsNull()
    {
        var result = ((SearchOptionsDto)null!).ToSearchOptions();

        result.Should().BeEquivalentTo(new SearchOptions());
    }

    [Fact]
    public void WhenToSearchOptionsAndAllUndefined_ThenReturnsSearchOptions()
    {
        var result = _hasSearchOptions.ToSearchOptions();

        result.Limit.Should().Be(SearchOptions.DefaultLimit);
        result.Offset.Should().Be(SearchOptions.NoOffset);
        result.Sort.HasValue.Should().BeFalse();
        result.Filter.Fields.Count.Should().Be(0);
    }

    [Fact]
    public void WhenToSearchOptionsAndLimit_ThenReturnsSearchOptions()
    {
        _hasSearchOptions.Limit = 9;
        _hasSearchOptions.Offset = 99;

        var result = _hasSearchOptions.ToSearchOptions();

        result.Limit.Should().Be(9);
        result.Offset.Should().Be(99);
        result.Sort.HasValue.Should().BeFalse();
        result.Filter.Fields.Count.Should().Be(0);
    }

    [Fact]
    public void WhenToSearchOptionsAndNoLimit_ThenReturnsSearchOptions()
    {
        _hasSearchOptions.Limit = SearchOptions.NoLimit;
        _hasSearchOptions.Offset = 99;

        var result = _hasSearchOptions.ToSearchOptions();

        result.Limit.Should().Be(SearchOptions.DefaultLimit);
        result.Offset.Should().Be(99);
        result.Sort.HasValue.Should().BeFalse();
        result.Filter.Fields.Count.Should().Be(0);
    }

    [Fact]
    public void WhenToSearchOptionsAndSingleSort_ThenReturnsSearchOptions()
    {
        _hasSearchOptions.Sort = "+Field1";

        var result = _hasSearchOptions.ToSearchOptions();

        result.Sort.Value.By.Should().Be("Field1");
        result.Sort.Value.Direction.Should().Be(SortDirection.Ascending);

        _hasSearchOptions.Sort = "-Field1";

        result = _hasSearchOptions.ToSearchOptions();

        result.Sort.Value.By.Should().Be("Field1");
        result.Sort.Value.Direction.Should().Be(SortDirection.Descending);
    }

    [Fact]
    public void WhenToSearchOptionsAndFilters_ThenReturnsSearchOptions()
    {
        _hasSearchOptions.Filter = "Field1;Field2";

        var result = _hasSearchOptions.ToSearchOptions();

        result.Filter.Fields.Count.Should().Be(2);
        result.Filter.Fields[0]
            .Should().Be("Field1");
        result.Filter.Fields[1]
            .Should().Be("Field2");
    }

    [Fact]
    public void WhenToSearchOptionsAndAllUndefinedWithDefaults_ThenReturnsSearchOptions()
    {
        var result = _hasSearchOptions.ToSearchOptions(9, 99, "-asort", "afilter");

        result.Limit.Should().Be(9);
        result.Offset.Should().Be(99);
        result.Sort.Value.By.Should().Be("asort");
        result.Sort.Value.Direction.Should().Be(SortDirection.Descending);
        result.Filter.Fields.Count.Should().Be(1);
        result.Filter.Fields[0]
            .Should().Be("afilter");
    }

    [Fact]
    public void WhenToSearchOptionsAndAllUndefinedWithDefaultMaxLimit_ThenReturnsSearchOptions()
    {
        var result = _hasSearchOptions.ToSearchOptions(0, 99, "-asort", "afilter");

        result.Limit.Should().Be(SearchOptions.DefaultLimit);
        result.Offset.Should().Be(99);
        result.Sort.Value.By.Should().Be("asort");
        result.Sort.Value.Direction.Should().Be(SortDirection.Descending);
        result.Filter.Fields.Count.Should().Be(1);
        result.Filter.Fields[0]
            .Should().Be("afilter");
    }

    [Fact]
    public void WhenToSearchOptionsWithDefaults_ThenReturnsSearchOptions()
    {
        _hasSearchOptions.Limit = 6;
        _hasSearchOptions.Offset = 66;
        _hasSearchOptions.Sort = "-asort1";
        _hasSearchOptions.Filter = "afilter1";

        var result = _hasSearchOptions.ToSearchOptions(9, 99, "asort2", "afilter2");

        result.Limit.Should().Be(6);
        result.Offset.Should().Be(66);
        result.Sort.Value.By.Should().Be("asort1");
        result.Sort.Value.Direction.Should().Be(SortDirection.Descending);
        result.Filter.Fields.Count.Should().Be(1);
        result.Filter.Fields[0]
            .Should().Be("afilter1");
    }

    [Fact]
    public void WhenWithOptionsAndNoOptions_ThenReturnsDto()
    {
        var result = new SearchOptionsDto()
            .WithOptions(new SearchOptions(), new GetOptions());

        result.Sort.Should().BeNull();
        result.Embed.Should().BeNull();
        result.Filter.Should().BeNull();
        result.Offset.Should().Be(-1);
    }

    [Fact]
    public void WhenWithOptionsAndAllOptions_ThenReturnsDto()
    {
        var result = new SearchOptionsDto()
            .WithOptions(new SearchOptions
            {
                Filter = new Filtering(["afield1", "afield2"]),
                Sort = new Sorting("asort", SortDirection.Descending),
                Limit = 9,
                Offset = 7
            }, new GetOptions(ExpandOptions.Custom, ["anembed1", "anembed2"], ExpandOptions.None));

        result.Sort.Should().Be("-asort");
        result.Embed.Should().Be("anembed1,anembed2");
        result.Filter.Should().Be("afield1,afield2");
        result.Limit.Should().Be(9);
        result.Offset.Should().Be(7);
    }
}

public class SearchOptionsDto : IHasSearchOptions
{
    public string? Embed { get; set; }

    public string? Filter { get; set; }

    public int? Limit { get; set; }

    public int? Offset { get; set; }

    public string? Sort { get; set; }
}