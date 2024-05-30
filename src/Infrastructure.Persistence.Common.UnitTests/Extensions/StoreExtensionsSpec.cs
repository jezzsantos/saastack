using Common;
using Domain.Interfaces;
using FluentAssertions;
using Infrastructure.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;
using QueryAny;
using UnitTesting.Common;
using Xunit;

namespace Infrastructure.Persistence.Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class StoreExtensionsSpec
{
    [Fact]
    public void WhenComplexTypeFromContainerPropertyWithNone_ThenReturnsNone()
    {
        var result = Optional<string>.None
            .ComplexTypeFromContainerProperty(typeof(string));

        result.Should().BeNone();
    }

    [Fact]
    public void WhenComplexTypeFromContainerPropertyWithEmpty_ThenReturnsNone()
    {
        var result = string.Empty.ToOptional()
            .ComplexTypeFromContainerProperty(typeof(string));

        result.Should().BeNone();
    }

    [Fact]
    public void WhenComplexTypeFromContainerPropertyAndTargetTypeIsNotComplexType_ThenReturnsSome()
    {
        var result = "avalue".ToOptional()
            .ComplexTypeFromContainerProperty(typeof(string));

        result.Should().BeSome("avalue");
    }

    [Fact]
    public void WhenComplexTypeFromContainerPropertyAndTargetTypeIsComplexTypeAndValueIsNotJson_ThenReturnsNone()
    {
        var result = "{notvalidjson}".ToOptional()
            .ComplexTypeFromContainerProperty(typeof(TestComplexType));

        result.Should().BeNone();
    }

    [Fact]
    public void
        WhenComplexTypeFromContainerPropertyAndTargetTypeIsComplexTypeAndValueIsEmptyJson_ThenReturnsDefaultInstance()
    {
        var result = "{}".ToOptional()
            .ComplexTypeFromContainerProperty(typeof(TestComplexType));

        result.Value.Should().BeEquivalentTo(new TestComplexType());
    }

    [Fact]
    public void WhenComplexTypeFromContainerPropertyAndTargetTypeIsComplexTypeAndValueIsJsonValue_ThenReturnsSome()
    {
        var result = $"{{\"{nameof(TestComplexType.AProperty)}\":\"avalue\"}}".ToOptional()
            .ComplexTypeFromContainerProperty(typeof(TestComplexType));

        result.Value.Should().BeEquivalentTo(new TestComplexType { AProperty = "avalue" });
    }

    [Fact]
    public void WhenComplexTypeToContainerPropertyWithNone_ThenReturnsNone()
    {
        var result = Optional<string>.None
            .ComplexTypeToContainerProperty();

        result.Should().BeNone();
    }

    [Fact]
    public void WhenComplexTypeToContainerPropertyWithAnEmptyStringValue_ThenReturnsStringValue()
    {
        var result = string.Empty.ToOptional()
            .ComplexTypeToContainerProperty();

        result.Should().BeSome(string.Empty);
    }

    [Fact]
    public void WhenComplexTypeToContainerPropertyWithASupportedPrimitiveValue_ThenReturnsStringValue()
    {
        var result = 99.ToOptional()
            .ComplexTypeToContainerProperty();

        result.Should().BeSome("99");
    }

    [Fact]
    public void
        WhenComplexTypeToContainerPropertyWithAComplexTypeWithOverwrittenToStringMethodValue_ThenReturnsToStringValue()
    {
        var result = new TestComplexTypeWithOverwrittenToString { AProperty = "avalue" }.ToOptional()
            .ComplexTypeToContainerProperty();

        result.Should().BeSome("overwritten");
    }

    [Fact]
    public void WhenComplexTypeToContainerPropertyWithAComplexTypeValue_ThenReturnsStringifiedValue()
    {
        var result = new TestComplexType { AProperty = "avalue" }.ToOptional()
            .ComplexTypeToContainerProperty();

        result.Should().BeSome($"{{\"{nameof(TestComplexType.AProperty)}\":\"avalue\"}}");
    }

    [Fact]
    public async Task WhenFetchAllIntoMemoryAsync_ThenReturnsResults()
    {
        var query = Query.From<TestDto>().WhereAll();
        var dtoProperties = HydrationProperties.FromDto(new TestDto { Id = "anid" });
        var metadata = PersistedEntityMetadata.FromType<TestDto>();
        var primaryEntities = new Dictionary<string, HydrationProperties>
        {
            { "anid", dtoProperties }
        };
        var joinedEntities = new Dictionary<string, HydrationProperties>();

        var result =
            await query.FetchAllIntoMemoryAsync(10, metadata,
                () => Task.FromResult(primaryEntities),
                _ => Task.FromResult(joinedEntities));

        result.Count.Should().Be(1);
        result[0].Id.Should().Be("anid");
    }

    [Fact]
    public void WhenGetDefaultOrderingForEntityWithoutIdAndNoOrderingSpecified_ThenReturnsLastPersistedAtUtc()
    {
        var query = Query.From<TestQueryEntityWithoutId>().WhereAll();

        var result = query.GetDefaultOrdering();

        result.Should().Be(StoreExtensions.DefaultOrderingPropertyName);
    }

    [Fact]
    public void
        WhenGetDefaultOrderingForEntityWithoutIdAndNoOrderingSpecifiedAndDefaultsNotSelected_ThenReturnsFirstSelectedField()
    {
        var query = Query.From<TestQueryEntityWithoutId>()
            .WhereAll()
            .Select(x => x.AProperty);

        var result = query.GetDefaultOrdering();

        result.Should().Be(nameof(TestQueryEntityWithoutId.AProperty));
    }

    [Fact]
    public void WhenGetDefaultOrderingForEntityWithoutLastPersistedAtUtcAndNoOrderingSpecified_ThenReturnsId()
    {
        var query = Query.From<TestQueryEntityWithId>().WhereAll();

        var result = query.GetDefaultOrdering();

        result.Should().Be(StoreExtensions.BackupOrderingPropertyName);
    }

    [Fact]
    public void
        WhenGetDefaultOrderingForEntityWithoutLastPersistedAtUtcAndNoOrderingSpecifiedAndDefaultsNotSelected_ThenReturnsId()
    {
        var query = Query.From<TestQueryEntityWithId>()
            .WhereAll()
            .Select(x => x.AProperty);

        var result = query.GetDefaultOrdering();

        result.Should().Be(nameof(TestQueryEntityWithId.AProperty));
    }

    [Fact]
    public void WhenGetDefaultSkipAndDefaultOffset_ThenReturnsZero()
    {
        var query = Query.From<TestQueryEntityWithId>()
            .WhereAll()
            .Skip(ResultOptions.DefaultOffset);

        var result = query.GetDefaultSkip();

        result.Should().Be(0);
    }

    [Fact]
    public void WhenGetDefaultSkipAndNoOffset_ThenReturnsZero()
    {
        var query = Query.From<TestQueryEntityWithId>()
            .WhereAll();

        var result = query.GetDefaultSkip();

        result.Should().Be(0);
    }

    [Fact]
    public void WhenGetDefaultSkipAndSomeOffset_ThenReturnsOffset()
    {
        var query = Query.From<TestQueryEntityWithId>()
            .WhereAll()
            .Skip(1);

        var result = query.GetDefaultSkip();

        result.Should().Be(1);
    }

    [Fact]
    public void WhenGetDefaultTakeAndNoLimit_ThenReturnsMax()
    {
        var query = Query.From<TestQueryEntityWithId>()
            .WhereAll();

        var result = query.GetDefaultTake(99);

        result.Should().Be(99);
    }

    [Fact]
    public void WhenGetDefaultTakeAndSomeLimit_ThenReturnsLimit()
    {
        var query = Query.From<TestQueryEntityWithId>()
            .WhereAll()
            .Take(1);

        var result = query.GetDefaultTake(99);

        result.Should().Be(1);
    }
}

public class TestComplexType
{
    public string? AProperty { get; set; }
}

public class TestComplexTypeWithOverwrittenToString
{
    public string? AProperty { get; set; }

    public override string ToString()
    {
        return "overwritten";
    }
}

[UsedImplicitly]
public class TestQueryEntityWithoutId : IQueryableEntity
{
    public string? AProperty { get; set; }

    public DateTime? LastPersistedAtUtc { get; set; }
}

[UsedImplicitly]
public class TestQueryEntityWithId : IQueryableEntity
{
    public string? AProperty { get; set; }

    public string? Id { get; set; }
}