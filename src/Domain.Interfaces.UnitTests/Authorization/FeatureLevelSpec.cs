using Domain.Interfaces.Authorization;
using FluentAssertions;
using Xunit;

namespace Domain.Interfaces.UnitTests.Authorization;

[Trait("Category", "Unit")]
public class FeatureLevelSpec
{
    [Fact]
    public void WhenEqualsAndNamesDifferent_ThenReturnsFalse()
    {
        var feature1 = new FeatureLevel("afeature1");
        var feature2 = new FeatureLevel("afeature2");

        var result = feature1.Equals(feature2);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsAndNamesSameButNoChildren_ThenReturnsTrue()
    {
        var feature1 = new FeatureLevel("afeature1");
        var feature2 = new FeatureLevel("afeature1");

        var result = feature1.Equals(feature2);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsAndNamesSameButOneHasChild_ThenReturnsFalse()
    {
        var feature1 = new FeatureLevel("afeature1");
        var feature2 = new FeatureLevel("afeature1", new FeatureLevel[] { new("achild1") });

        var result = feature1.Equals(feature2);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsAndNamesSameButOneHasGrandChild_ThenReturnsFalse()
    {
        var feature1 = new FeatureLevel("afeature1", new FeatureLevel[] { new("achild1") });
        var feature2 = new FeatureLevel("afeature1",
            new FeatureLevel[] { new("achild1", new FeatureLevel[] { new("achild2") }) });

        var result = feature1.Equals(feature2);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsAndNamesSameAndSameChild_ThenReturnsTrue()
    {
        var feature1 = new FeatureLevel("afeature1", new FeatureLevel[] { new("achild1") });
        var feature2 = new FeatureLevel("afeature1", new FeatureLevel[] { new("achild1") });

        var result = feature1.Equals(feature2);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsAndNamesSameAndDifferentAncestors_ThenReturnsFalse()
    {
        var feature1 = new FeatureLevel("afeature1",
            new FeatureLevel[] { new("achild1", new FeatureLevel[] { new("achild2") }) });
        var feature2 = new FeatureLevel("afeature1",
            new FeatureLevel[] { new("achild1", new FeatureLevel[] { new("achild3") }) });

        var result = feature1.Equals(feature2);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsAndNamesSameAndSameAncestors_ThenReturnsTrue()
    {
        var feature1 = new FeatureLevel("afeature1",
            new FeatureLevel[] { new("achild1", new FeatureLevel[] { new("achild2") }) });
        var feature2 = new FeatureLevel("afeature1",
            new FeatureLevel[] { new("achild1", new FeatureLevel[] { new("achild2") }) });

        var result = feature1.Equals(feature2);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasDescendantAndHasNoChild_ThenFalse()
    {
        var feature = new FeatureLevel("afeature1");

        var result = feature.HasDescendant(new FeatureLevel("achild"));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasDescendantAndHasChild_ThenTrue()
    {
        var feature = new FeatureLevel("afeature1", new FeatureLevel[]
        {
            new("achild")
        });

        var result = feature.HasDescendant(new FeatureLevel("achild"));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasDescendantAndHasNoChild2_ThenFalse()
    {
        var feature = new FeatureLevel("afeature1", new FeatureLevel[]
        {
            new("afeature2")
        });

        var result = feature.HasDescendant(new FeatureLevel("achild2"));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasDescendantAndHasGrandChild_ThenTrue()
    {
        var feature = new FeatureLevel("afeature1",
            new FeatureLevel[]
            {
                new("afeature2", new FeatureLevel[]
                {
                    new("achild")
                })
            });

        var result = feature.HasDescendant(new FeatureLevel("achild"));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasDescendantAndHasGreatGrandChild_ThenTrue()
    {
        var feature = new FeatureLevel("afeature1",
            new FeatureLevel[]
            {
                new("afeature2", new FeatureLevel[]
                {
                    new("afeature3", new FeatureLevel[]
                    {
                        new("achild")
                    })
                })
            });

        var result = feature.HasDescendant(new FeatureLevel("achild"));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasDescendantAndHasGreatGrandChild2_ThenTrue()
    {
        var feature = new FeatureLevel("afeature1",
            new FeatureLevel[]
            {
                new("afeature2",
                    new FeatureLevel[]
                    {
                        new("afeature3",
                            new FeatureLevel("achild1"),
                            new FeatureLevel("achild2"),
                            new FeatureLevel("achild3"))
                    })
            });

        var result = feature.HasDescendant(new FeatureLevel("achild2"));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenAllDescendantNames_ThenReturnsAllNames()
    {
        var feature = new FeatureLevel("afeature1",
            new FeatureLevel[]
            {
                new("afeature2",
                    new FeatureLevel[]
                    {
                        new("afeature3",
                            new FeatureLevel("achild1"),
                            new FeatureLevel("achild2"),
                            new FeatureLevel("achild3"))
                    })
            });

        var result = feature.AllDescendantNames();

        result.Should().ContainInOrder("afeature1", "afeature2", "afeature3", "achild1", "achild2", "achild3");
    }
}