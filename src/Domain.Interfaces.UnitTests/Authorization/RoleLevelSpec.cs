using Domain.Interfaces.Authorization;
using FluentAssertions;
using Xunit;

namespace Domain.Interfaces.UnitTests.Authorization;

[Trait("Category", "Unit")]
public class RoleLevelSpec
{
    [Fact]
    public void WhenEqualsAndNamesDifferent_ThenReturnsFalse()
    {
        var role1 = new RoleLevel("arole1");
        var role2 = new RoleLevel("arole2");

        var result = role1.Equals(role2);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsAndNamesSameButNoChildren_ThenReturnsTrue()
    {
        var role1 = new RoleLevel("arole1");
        var role2 = new RoleLevel("arole1");

        var result = role1.Equals(role2);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsAndNamesSameButOneHasChild_ThenReturnsFalse()
    {
        var role1 = new RoleLevel("arole1");
        var role2 = new RoleLevel("arole1", new RoleLevel[] { new("achild1") });

        var result = role1.Equals(role2);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsAndNamesSameButOneHasGrandChild_ThenReturnsFalse()
    {
        var role1 = new RoleLevel("arole1", new RoleLevel[] { new("achild1") });
        var role2 = new RoleLevel("arole1",
            new RoleLevel[] { new("achild1", new RoleLevel[] { new("achild2") }) });

        var result = role1.Equals(role2);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsAndNamesSameAndSameChild_ThenReturnsTrue()
    {
        var role1 = new RoleLevel("arole1", new RoleLevel[] { new("achild1") });
        var role2 = new RoleLevel("arole1", new RoleLevel[] { new("achild1") });

        var result = role1.Equals(role2);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenEqualsAndNamesSameAndDifferentAncestors_ThenReturnsFalse()
    {
        var role1 = new RoleLevel("arole1",
            new RoleLevel[] { new("achild1", new RoleLevel[] { new("achild2") }) });
        var role2 = new RoleLevel("arole1",
            new RoleLevel[] { new("achild1", new RoleLevel[] { new("achild3") }) });

        var result = role1.Equals(role2);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenEqualsAndNamesSameAndSameAncestors_ThenReturnsTrue()
    {
        var role1 = new RoleLevel("arole1",
            new RoleLevel[] { new("achild1", new RoleLevel[] { new("achild2") }) });
        var role2 = new RoleLevel("arole1",
            new RoleLevel[] { new("achild1", new RoleLevel[] { new("achild2") }) });

        var result = role1.Equals(role2);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasDescendantAndHasNoChild_ThenFalse()
    {
        var role = new RoleLevel("arole1");

        var result = role.HasDescendant(new RoleLevel("achild"));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasDescendantAndHasChild_ThenTrue()
    {
        var role = new RoleLevel("arole1", new RoleLevel[]
        {
            new("achild")
        });

        var result = role.HasDescendant(new RoleLevel("achild"));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasDescendantAndHasNoChild2_ThenFalse()
    {
        var role = new RoleLevel("arole1", new RoleLevel[]
        {
            new("arole2")
        });

        var result = role.HasDescendant(new RoleLevel("achild2"));

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasDescendantAndHasGrandChild_ThenTrue()
    {
        var role = new RoleLevel("arole1",
            new RoleLevel[]
            {
                new("arole2", new RoleLevel[]
                {
                    new("achild")
                })
            });

        var result = role.HasDescendant(new RoleLevel("achild"));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasDescendantAndHasGreatGrandChild_ThenTrue()
    {
        var role = new RoleLevel("arole1",
            new RoleLevel[]
            {
                new("arole2", new RoleLevel[]
                {
                    new("arole3", new RoleLevel[]
                    {
                        new("achild")
                    })
                })
            });

        var result = role.HasDescendant(new RoleLevel("achild"));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasDescendantAndHasGreatGrandChild2_ThenTrue()
    {
        var role = new RoleLevel("arole1",
            new RoleLevel[]
            {
                new("arole2",
                    new RoleLevel[]
                    {
                        new("arole3",
                            new RoleLevel("achild1"),
                            new RoleLevel("achild2"),
                            new RoleLevel("achild3"))
                    })
            });

        var result = role.HasDescendant(new RoleLevel("achild2"));

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenAllDescendantNames_ThenReturnsAllNames()
    {
        var role = new RoleLevel("arole1",
            new RoleLevel[]
            {
                new("arole2",
                    new RoleLevel[]
                    {
                        new("arole3",
                            new RoleLevel("achild1"),
                            new RoleLevel("achild2"),
                            new RoleLevel("achild3"))
                    })
            });

        var result = role.AllDescendantNames();

        result.Should().ContainInOrder("arole1", "arole2", "arole3", "achild1", "achild2", "achild3");
    }
}