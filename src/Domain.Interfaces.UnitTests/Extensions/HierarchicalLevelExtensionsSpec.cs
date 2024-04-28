using Domain.Interfaces.Authorization;
using Domain.Interfaces.Extensions;
using FluentAssertions;
using Xunit;

namespace Domain.Interfaces.UnitTests.Extensions;

[Trait("Category", "Unit")]
public class HierarchicalLevelExtensionsSpec
{
    [Fact]
    public void WhenNormalizeWithNoLevels_ThenHasNoLevels()
    {
        var result = Array.Empty<TestLevel>().Normalize();

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenNormalizeSingleLevelLevel_ThenHasAllLevels()
    {
        var result = new[]
        {
            new TestLevel("arole1")
        }.Normalize();

        result.Length.Should().Be(1);
        result.Should().ContainInOrder(new TestLevel("arole1"));
    }

    [Fact]
    public void WhenNormalizeWithMultipleLowLevelLevels_ThenHasAllLevels()
    {
        var result = new[]
        {
            new TestLevel("arole1"), new TestLevel("arole2"), new TestLevel("arole3")
        }.Normalize();

        result.Length.Should().Be(3);
        result.Should().ContainInOrder(new TestLevel("arole1"), new TestLevel("arole2"), new TestLevel("arole3"));
    }

    [Fact]
    public void WhenNormalizeWithMultipleHighLevelLevels_ThenHasAllLevels()
    {
        var child1 = new TestLevel("achildrole1");
        var child2 = new TestLevel("achildrole2");
        var child3 = new TestLevel("achildrole3");
        var parent1WithChild1 = new TestLevel("aparentrole1", child1);
        var parent2WithChild2 = new TestLevel("aparentrole2", child2);
        var parent3WithChild3 = new TestLevel("aparentrole3", child3);
        var grandParent1WithParent1 = new TestLevel("agrandparentrole1", parent1WithChild1);
        var grandParent2WithParent2 = new TestLevel("agrandparentrole2", parent2WithChild2);
        var grandParent3WithParent3 = new TestLevel("agrandparentrole3", parent3WithChild3);

        var result = new[]
        {
            grandParent1WithParent1, grandParent2WithParent2, grandParent3WithParent3
        }.Normalize();

        result.Length.Should().Be(3);
        result.Should().ContainInOrder(grandParent1WithParent1, grandParent2WithParent2, grandParent3WithParent3);
    }

    [Fact]
    public void WhenNormalizeWithDifferentParentLevels_ThenHasOnlyParentLevels()
    {
        var child1 = new TestLevel("achildrole1");
        var child2 = new TestLevel("achildrole2");
        var child3 = new TestLevel("achildrole3");
        var parent1WithChild1 = new TestLevel("aparentrole1", child1);
        var parent2WithChild2 = new TestLevel("aparentrole2", child2);
        var parent3WithChild3 = new TestLevel("aparentrole3", child3);

        var result = new[]
        {
            parent1WithChild1,
            parent2WithChild2,
            parent3WithChild3
        }.Normalize();

        result.Length.Should().Be(3);
        result.Should().ContainInOrder(parent1WithChild1, parent2WithChild2, parent3WithChild3);
    }

    [Fact]
    public void WhenNormalizeWithParentAndChildLevelLevels_ThenHasOnlyParentLevels()
    {
        var child1 = new TestLevel("achildrole1");
        var child2 = new TestLevel("achildrole2");
        var child3 = new TestLevel("achildrole3");
        var parent1WithChild1 = new TestLevel("aparentrole1", child1);
        var parent2WithChild2 = new TestLevel("aparentrole2", child2);
        var parent3WithChild3 = new TestLevel("aparentrole3", child3);

        var result = new[]
        {
            parent1WithChild1, child1,
            parent2WithChild2, child2,
            parent3WithChild3, child3
        }.Normalize();

        result.Length.Should().Be(3);
        result.Should().ContainInOrder(parent1WithChild1, parent2WithChild2, parent3WithChild3);
    }

    [Fact]
    public void WhenNormalizeWithGrandParentAndDescendantLevelLevels_ThenHasOnlyGrandParentLevels()
    {
        var child1 = new TestLevel("achildrole1");
        var child2 = new TestLevel("achildrole2");
        var child3 = new TestLevel("achildrole3");
        var parent1WithChild1 = new TestLevel("aparentrole1", child1);
        var parent2WithChild2 = new TestLevel("aparentrole2", child2);
        var parent3WithChild3 = new TestLevel("aparentrole3", child3);
        var grandParent1WithParent1 = new TestLevel("agrandparentrole1", parent1WithChild1);
        var grandParent2WithParent2 = new TestLevel("agrandparentrole2", parent2WithChild2);
        var grandParent3WithParent3 = new TestLevel("agrandparentrole3", parent3WithChild3);

        var result = new[]
        {
            grandParent1WithParent1, parent1WithChild1, child1,
            grandParent2WithParent2, parent2WithChild2, child2,
            grandParent3WithParent3, parent3WithChild3, child3
        }.Normalize();

        result.Length.Should().Be(3);
        result.Should().ContainInOrder(grandParent1WithParent1, grandParent2WithParent2, grandParent3WithParent3);
    }

    [Fact]
    public void WhenNormalizeWithUniqueChildLevels_ThenHasAllLevels()
    {
        var child1 = new TestLevel("achildrole1");
        var child2 = new TestLevel("achildrole2");
        var child3 = new TestLevel("achildrole3");
        var parent1WithChild1 = new TestLevel("aparentrole1", child1);

        var result = new[]
        {
            parent1WithChild1, child1, child2, child3
        }.Normalize();

        result.Length.Should().Be(3);
        result.Should().ContainInOrder(parent1WithChild1, child2, child3);
    }

    [Fact]
    public void WhenDenormalizeAndNoLevels_TheNoLevels()
    {
        var result = Array.Empty<TestLevel>().Denormalize();

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenDenormalizeSingleLevel_TheHasAllLevels()
    {
        var result = new[]
        {
            new TestLevel("arole1")
        }.Denormalize();

        result.Length.Should().Be(1);
        result.Should().ContainInOrder("arole1");
    }

    [Fact]
    public void WhenDenormalizeSingleHighLevel_TheHasAllLevels()
    {
        var child1 = new TestLevel("achildrole1");
        var parent1WithChild1 = new TestLevel("aparentrole1", child1);
        var grandParent1WithParent1 = new TestLevel("agrandparentrole1", parent1WithChild1);

        var result = new[]
        {
            grandParent1WithParent1
        }.Denormalize();

        result.Length.Should().Be(3);
        result.Should().ContainInOrder("agrandparentrole1");
        result.Should().ContainInOrder("aparentrole1");
        result.Should().ContainInOrder("achildrole1");
    }

    [Fact]
    public void WhenDenormalizeWithMultipleLowLevelLevels_ThenHasAllLevels()
    {
        var result = new[]
        {
            new TestLevel("arole1"), new TestLevel("arole2"), new TestLevel("arole3")
        }.Denormalize();

        result.Length.Should().Be(3);
        result.Should().ContainInOrder("arole1", "arole2", "arole3");
    }

    [Fact]
    public void WhenDenormalizeWithMultipleHighLevelLevels_ThenHasAllDescendantLevels()
    {
        var child1 = new TestLevel("achildrole1");
        var child2 = new TestLevel("achildrole2");
        var child3 = new TestLevel("achildrole3");
        var parent1WithChild1 = new TestLevel("aparentrole1", child1);
        var parent2WithChild2 = new TestLevel("aparentrole2", child2);
        var parent3WithChild3 = new TestLevel("aparentrole3", child3);
        var grandParent1WithParent1 = new TestLevel("agrandparentrole1", parent1WithChild1);
        var grandParent2WithParent2 = new TestLevel("agrandparentrole2", parent2WithChild2);
        var grandParent3WithParent3 = new TestLevel("agrandparentrole3", parent3WithChild3);

        var result = new[]
        {
            grandParent1WithParent1, grandParent2WithParent2, grandParent3WithParent3
        }.Denormalize();

        result.Length.Should().Be(9);
        result.Should().ContainInOrder("agrandparentrole1", "aparentrole1", "achildrole1", "agrandparentrole2",
            "aparentrole2", "achildrole2", "agrandparentrole3", "aparentrole3", "achildrole3");
    }

    [Fact]
    public void WhenDenormalizeWithUniqueChildLevels_ThenHasAllLevels()
    {
        var child1 = new TestLevel("achildrole1");
        var child2 = new TestLevel("achildrole2");
        var child3 = new TestLevel("achildrole3");
        var parent1WithChild1 = new TestLevel("aparentrole1", child1);

        var result = new[]
        {
            parent1WithChild1, child1, child2, child3
        }.Denormalize();

        result.Length.Should().Be(4);
        result.Should().ContainInOrder("aparentrole1", "achildrole1", "achildrole2", "achildrole3");
    }

    [Fact]
    public void WhenMergeAndEmpty_ThenReturnsAdded()
    {
        var result = Array.Empty<TestLevel>()
            .Merge(new TestLevel("arole1"));

        result.Length.Should().Be(1);
        result.Should().ContainInOrder(new TestLevel("arole1"));
    }

    [Fact]
    public void WhenMergeAndExists_ThenReturnsOriginal()
    {
        var result = new[]
            {
                new TestLevel("arole")
            }
            .Merge(new TestLevel("arole"));

        result.Length.Should().Be(1);
        result.Should().ContainInOrder(new TestLevel("arole"));
    }

    [Fact]
    public void WhenMergeAndNotExists_ThenReturnsAdded()
    {
        var result = new[]
            {
                new TestLevel("arole1")
            }
            .Merge(new TestLevel("arole2"));

        result.Length.Should().Be(2);
        result.Should().ContainInOrder(new TestLevel("arole1"), new TestLevel("arole2"));
    }

    [Fact]
    public void WhenMergeDescendantAndAncestorAlreadyExists_ThenReturnsAncestor()
    {
        var child = new TestLevel("achildrole");
        var parentWithChild = new TestLevel("aparentrole", child);
        var grandParentWithChild = new TestLevel("agrandparentrole", parentWithChild);

        var result = new[]
            {
                grandParentWithChild
            }
            .Merge(child);

        result.Length.Should().Be(1);
        result.Should().ContainInOrder(grandParentWithChild);
    }

    [Fact]
    public void WhenMergeAncestorAndDescendantAlreadyExists_ThenReturnsAncestor()
    {
        var child = new TestLevel("achildrole");
        var parentWithChild = new TestLevel("aparentrole", child);
        var grandParentWithChild = new TestLevel("agrandparentrole", parentWithChild);

        var result = new[]
            {
                child
            }
            .Merge(grandParentWithChild);

        result.Length.Should().Be(1);
        result.Should().ContainInOrder(grandParentWithChild);
    }

    [Fact]
    public void WhenUnMergeAndEmpty_ThenReturnsEmpty()
    {
        var result = Array.Empty<TestLevel>()
            .UnMerge(new TestLevel("arole1"));

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenUnMergeAndExists_ThenRemoves()
    {
        var result = new[]
            {
                new TestLevel("arole")
            }
            .UnMerge(new TestLevel("arole"));

        result.Should().BeEmpty();
    }

    [Fact]
    public void WhenUnMergeDescendantAndAncestorExists_ThenReturnsOriginal()
    {
        var child = new TestLevel("achildrole");
        var parentWithChild = new TestLevel("aparentrole", child);
        var grandParentWithChild = new TestLevel("agrandparentrole", parentWithChild);

        var result = new[]
            {
                grandParentWithChild
            }
            .UnMerge(child);

        result.Length.Should().Be(1);
        result.Should().ContainInOrder(grandParentWithChild);
    }

    [Fact]
    public void WhenUnMergeAncestorAndHasSingleDescendant_ThenReturnsDescendant()
    {
        var child = new TestLevel("achildrole");
        var parentWithChild = new TestLevel("aparentrole", child);
        var grandParentWithChild = new TestLevel("agrandparentrole", parentWithChild);

        var result = new[]
            {
                grandParentWithChild
            }
            .UnMerge(grandParentWithChild);

        result.Length.Should().Be(1);
        result.Should().ContainInOrder(parentWithChild);
    }

    [Fact]
    public void WhenUnMergeAncestorAndHasMultipleDescendants_ThenReturnsAllDescendants()
    {
        var child1 = new TestLevel("achildrole1");
        var parentWithChild1 = new TestLevel("aparentrole1", child1);
        var child2 = new TestLevel("achildrole2");
        var parentWithChild2 = new TestLevel("aparentrole2", child2);
        var grandParentWithChild = new TestLevel("agrandparentrole", [parentWithChild1, parentWithChild2]);

        var result = new[]
            {
                grandParentWithChild
            }
            .UnMerge(grandParentWithChild);

        result.Length.Should().Be(2);
        result.Should().ContainInOrder(parentWithChild1, parentWithChild2);
    }
}

public class TestLevel : HierarchicalLevelBase<TestLevel>
{
    public TestLevel(string name) : this(name, [])
    {
    }

    public TestLevel(string name, HierarchicalLevelBase<TestLevel> child) : this(name, [child])
    {
    }

    public TestLevel(string name, HierarchicalLevelBase<TestLevel>[] children) : base(name, children)
    {
    }
}