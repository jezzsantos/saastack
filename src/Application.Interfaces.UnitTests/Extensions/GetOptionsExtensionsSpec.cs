using Application.Interfaces.Extensions;
using FluentAssertions;
using JetBrains.Annotations;
using Xunit;

namespace Application.Interfaces.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class GetOptionsExtensionsSpec
{
    [Fact]
    public void WhenShouldExpandEmbeddedResourceAndNullOptions_ThenReturnsTrue()
    {
        var result = ((GetOptions)null!).ShouldExpandEmbeddedResource<TestResource>(x => x.AProperty1);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenShouldExpandEmbeddedResourceAndExpandIsAll_ThenReturnsTrue()
    {
        var result = new GetOptions(ExpandOptions.All).ShouldExpandEmbeddedResource<TestResource>(x => x.AProperty1);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenShouldExpandEmbeddedResourceAndExpandIsNone_ThenReturnsFalse()
    {
        var result = new GetOptions(ExpandOptions.None).ShouldExpandEmbeddedResource<TestResource>(x => x.AProperty1);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenShouldExpandEmbeddedResourceAndExpandIsCustomAndNoChildResources_ThenReturnsFalse()
    {
        var result =
            new GetOptions(ExpandOptions.Custom, new List<string>()).ShouldExpandEmbeddedResource<TestResource>(x =>
                x.AProperty1);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenShouldExpandEmbeddedResourceAndExpandIsCustomAndUnknownChildResources_ThenReturnsFalse()
    {
        var result =
            new GetOptions(ExpandOptions.Custom, new List<string> { "TestResource.AnotherProperty" })
                .ShouldExpandEmbeddedResource<TestResource>(x => x.AProperty1);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenShouldExpandEmbeddedResourceAndExpandIsCustomAndKnownChildResources_ThenReturnsTrue()
    {
        var result =
            new GetOptions(ExpandOptions.Custom, new List<string> { "TestResource.AProperty1" })
                .ShouldExpandEmbeddedResource<TestResource>(x => x.AProperty1);

        result.Should().BeTrue();
    }
}

[UsedImplicitly]
public class TestResource
{
    public string? AProperty1 { get; set; }

    public string? AProperty2 { get; set; }
}