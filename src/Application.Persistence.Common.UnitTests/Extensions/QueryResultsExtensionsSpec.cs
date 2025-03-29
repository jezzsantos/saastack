using Application.Interfaces;
using Application.Persistence.Common.Extensions;
using Application.Persistence.Interfaces;
using FluentAssertions;
using QueryAny;
using Xunit;

namespace Application.Persistence.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class QueryResultsExtensionsSpec
{
    private readonly SearchOptions _searchOptions = new();

    [Fact]
    public void WhenToSearchResults_ThenReturnsResults()
    {
        var sortable = new Sortable { AProperty = 1 };
        var results = new QueryResults<Sortable>([sortable], 1);

        var result = results.ToSearchResults(_searchOptions);

        result.Results.Should().HaveCount(1);
        result.Results.First().Should().Be(sortable);
        result.Metadata.Total.Should().Be(1);
    }
}

public class Sortable : IQueryableEntity
{
    public int AProperty { get; set; }
}