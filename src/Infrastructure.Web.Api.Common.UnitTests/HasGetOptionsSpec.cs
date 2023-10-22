using Application.Interfaces;
using FluentAssertions;
using Infrastructure.Web.Api.Interfaces;
using JetBrains.Annotations;
using Xunit;

namespace Infrastructure.Web.Api.Common.UnitTests;

[Trait("Category", "Unit")]
public class HasGetOptionsSpec
{
    [Fact]
    public void WhenAll_ThenReturnsAll()
    {
        var result = HasGetOptions.All.ToGetOptions();

        result.Expand.Should().Be(ExpandOptions.All);
    }

    [Fact]
    public void WhenNone_ThenReturnsNone()
    {
        var result = HasGetOptions.None.ToGetOptions();

        result.Expand.Should().Be(ExpandOptions.None);
    }

    [Fact]
    public void WhenCustomWithSingleResourceReference_ThenReturnsChildResources()
    {
        var result = HasGetOptions.Custom<TestResource>(x => x.AProperty1)
            .ToGetOptions();

        result.Expand.Should().Be(ExpandOptions.Custom);
        result.ResourceReferences.Count()
            .Should().Be(1);
        result.ResourceReferences.ToList()[0]
            .Should().Be("testresource.aproperty1");
    }

    [Fact]
    public void WhenCustomWithMultipleResourceReferences_ThenReturnsChildResources()
    {
        var result = HasGetOptions.Custom<TestResource>(x => x.AProperty1, x => x.AProperty2)
            .ToGetOptions();

        result.Expand.Should().Be(ExpandOptions.Custom);
        result.ResourceReferences.Count()
            .Should().Be(2);
        result.ResourceReferences.ToList()[0]
            .Should().Be("testresource.aproperty1");
        result.ResourceReferences.ToList()[1]
            .Should().Be("testresource.aproperty2");
    }
}

[UsedImplicitly]
public class TestResource
{
    public string? AProperty1 { get; set; }

    public string? AProperty2 { get; set; }
}

public class GetOptionsDto : IHasGetOptions
{
    public string? Embed { get; set; }
}