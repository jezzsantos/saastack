using Common.Extensions;
using FluentAssertions;
using Xunit;

namespace Common.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class DictionaryExtensionsSpec
{
    [Fact]
    public void WhenMergeAndSourceAndOtherIsEmpty_ThenContainsNothing()
    {
        var source = new Dictionary<string, string>();

        source.Merge(new Dictionary<string, string>());

        source.Count.Should()
            .Be(0);
    }

    [Fact]
    public void WhenMergeAndOtherIsEmpty_ThenNothingAdded()
    {
        var source = new Dictionary<string, string>
        {
            { "aname", "avalue" }
        };

        source.Merge(new Dictionary<string, string>());

        source.Count.Should()
            .Be(1);
        source.Should()
            .OnlyContain(pair => pair.Key == "aname");
    }

    [Fact]
    public void WhenMergeAndSourceIsEmpty_ThenOtherAdded()
    {
        var source = new Dictionary<string, string>();

        source.Merge(new Dictionary<string, string>
        {
            { "aname", "avalue" }
        });

        source.Count.Should()
            .Be(1);
        source.Should()
            .OnlyContain(pair => pair.Key == "aname");
    }

    [Fact]
    public void WhenMergeAndSourceAndOtherHaveUniqueKeys_ThenOtherAdded()
    {
        var source = new Dictionary<string, string>
        {
            { "aname1", "avalue1" }
        };

        source.Merge(new Dictionary<string, string>
        {
            { "aname2", "avalue2" }
        });

        source.Count.Should()
            .Be(2);
        source.Should()
            .Contain(pair => pair.Key == "aname1");
        source.Should()
            .Contain(pair => pair.Key == "aname2");
    }

    [Fact]
    public void WhenMergeAndSourceAndOtherHaveSameKeys_ThenSourceRemains()
    {
        var source = new Dictionary<string, string>
        {
            { "aname1", "avalue1" },
            { "aname2", "avalue2" }
        };

        source.Merge(new Dictionary<string, string>
        {
            { "aname2", "avalue2" },
            { "aname3", "avalue3" }
        });

        source.Count.Should()
            .Be(3);
        source.Should()
            .Contain(pair => pair.Key == "aname1");
        source.Should()
            .Contain(pair => pair.Key == "aname2");
        source.Should()
            .Contain(pair => pair.Key == "aname3");
    }
}