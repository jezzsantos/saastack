using FluentAssertions;
using Xunit;

namespace Common.UnitTests;

[Trait("Category", "Unit")]
public class SubscriptionMetadataSpec
{
    [Fact]
    public void WhenConstructedWithDictionary_ThenInitialized()
    {
        var result = new SubscriptionMetadata
        {
            { "aname", "avalue" }
        };

        result.Count.Should().Be(1);
        result["aname"].Should().Be("avalue");
    }

    [Fact]
    public void WhenEqualsAndNull_ThenReturnsFalse()
    {
        var result = new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }.Equals(null);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsAndOneEmpty_ThenReturnsFalse()
    {
        var result = new SubscriptionMetadata
        {
            { "aname", "avalue" }
        }.Equals(new SubscriptionMetadata());

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsAndSameKeysDifferentValues_ThenReturnsFalse()
    {
        var result = new SubscriptionMetadata
        {
            { "aname", "avalue1" }
        }.Equals(new SubscriptionMetadata
        {
            { "aname", "avalue2" }
        });

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsAndDifferentKeysSameValues_ThenReturnsFalse()
    {
        var result = new SubscriptionMetadata
        {
            { "aname1", "avalue" }
        }.Equals(new SubscriptionMetadata
        {
            { "aname2", "avalue" }
        });

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsAndDifferentKeysDifferentValues_ThenReturnsFalse()
    {
        var result = new SubscriptionMetadata
        {
            { "aname1", "avalue1" }
        }.Equals(new SubscriptionMetadata
        {
            { "aname2", "avalue2" }
        });

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsAndBothEmpty_ThenReturnsTrue()
    {
        var result = new SubscriptionMetadata().Equals(new SubscriptionMetadata());

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsAndSameKeysSameValuesSingle_ThenReturnsTrue()
    {
        var result = new SubscriptionMetadata
        {
            { "aname", "avalue1" }
        }.Equals(new SubscriptionMetadata
        {
            { "aname", "avalue1" }
        });

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsAndSameKeysSameValuesMultipleUnordered_ThenReturnsTrue()
    {
        var result = new SubscriptionMetadata
        {
            { "aname1", "avalue1" },
            { "aname2", "avalue2" },
            { "aname3", "avalue3" }
        }.Equals(new SubscriptionMetadata
        {
            { "aname3", "avalue3" },
            { "aname2", "avalue2" },
            { "aname1", "avalue1" }
        });

        result.Should().BeTrue();
    }
}