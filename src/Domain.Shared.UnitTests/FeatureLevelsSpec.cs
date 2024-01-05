using Common;
using Domain.Interfaces.Authorization;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests;

[Trait("Category", "Unit")]
public class FeatureLevelSpec
{
    [Fact]
    public void WhenCreateWithEmpty_ThenReturnsError()
    {
        var result = FeatureLevel.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreateWithInvalidName_ThenReturnsError()
    {
        var result = FeatureLevel.Create("^aninvalidname^");

        result.Should().BeError(ErrorCode.Validation, Resources.FeatureLevels_InvalidFeatureLevel);
    }

    [Fact]
    public void WhenCreateWithUnknownName_ThenReturnsError()
    {
        var result = FeatureLevel.Create("anunknownlevel");

        result.Should().BeError(ErrorCode.Validation, Resources.FeatureLevels_InvalidFeatureLevel);
    }

    [Fact]
    public void WhenCreateWithKnownName_ThenReturnsValue()
    {
        var result = FeatureLevel.Create(PlatformFeatureLevels.Basic.Name);

        result.Should().BeSuccess();
        result.Value.Identifier.Should().Be(PlatformFeatureLevels.Basic.Name);
    }
}

[Trait("Category", "Unit")]
public class FeatureLevelsSpec
{
    [Fact]
    public void WhenCreate_ThenReturnsError()
    {
        var result = FeatureLevels.Create();

        result.Should().BeSuccess();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreateWithSingleEmpty_ThenReturnsError()
    {
        var result = FeatureLevels.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreateWithSingle_ThenReturnsValue()
    {
        var result = FeatureLevels.Create(PlatformFeatureLevels.Basic.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(FeatureLevel.Create(PlatformFeatureLevels.Basic.Name).Value);
    }

    [Fact]
    public void WhenCreateWithEmptyList_ThenReturnsValue()
    {
        var result = FeatureLevels.Create(Enumerable.Empty<string>());

        result.Should().BeSuccess();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreateWithListContainingInvalidItem_ThenReturnsError()
    {
        var result = FeatureLevels.Create(new[] { PlatformFeatureLevels.Basic.Name, string.Empty });

        result.Should().BeError(ErrorCode.Validation);
    }

#if TESTINGONLY
    [Fact]
    public void WhenCreateWithListContainingValidItems_ThenReturnsValue()
    {
        var result = FeatureLevels.Create(new[]
            { PlatformFeatureLevels.Basic.Name, PlatformFeatureLevels.TestingOnlyLevel.Name });

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(FeatureLevel.Create(PlatformFeatureLevels.Basic.Name).Value,
            FeatureLevel.Create(PlatformFeatureLevels.TestingOnlyLevel.Name).Value);
    }
#endif

    [Fact]
    public void WhenAddStringAndInvalid_ThenReturnsError()
    {
        var levels = FeatureLevels.Create().Value;

        var result = levels.Add("anunknownlevel");

        result.Should().BeError(ErrorCode.Validation, Resources.FeatureLevels_InvalidFeatureLevel);
    }

    [Fact]
    public void WhenAddStringValid_ThenAddsFeatureLevel()
    {
        var levels = FeatureLevels.Create().Value;

        var result = levels.Add(PlatformFeatureLevels.Basic.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainSingle(lvl => lvl.Identifier == PlatformFeatureLevels.Basic.Name);
    }

    [Fact]
    public void WhenAddFeatureLevelAndExists_ThenDoesNotAdd()
    {
        var levels = FeatureLevels.Create().Value;
        levels.Add(PlatformFeatureLevels.Basic.Name);

        var result = levels.Add(PlatformFeatureLevels.Basic.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainSingle(lvl => lvl.Identifier == PlatformFeatureLevels.Basic.Name);
    }

#if TESTINGONLY
    [Fact]
    public void WhenAddFeatureLevelAndNotExists_ThenAdds()
    {
        var levels = FeatureLevels.Create().Value;
        levels = levels.Add(PlatformFeatureLevels.Basic.Name).Value;

        var result = levels.Add(PlatformFeatureLevels.TestingOnlyLevel.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(FeatureLevel.Create(PlatformFeatureLevels.Basic.Name).Value,
            FeatureLevel.Create(PlatformFeatureLevels.TestingOnlyLevel.Name).Value);
    }
#endif

    [Fact]
    public void WhenClear_ThenRemovesAllItems()
    {
        var levels = FeatureLevels.Create().Value;
        levels = levels.Add(PlatformFeatureLevels.Basic.Name).Value;

        var result = levels.Clear();

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenHasAnyAndSome_ThenReturnsTrue()
    {
        var levels = FeatureLevels.Create().Value;
        levels = levels.Add(PlatformFeatureLevels.Basic.Name).Value;

        var result = levels.HasAny();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasAnyAndNone_ThenReturnsFalse()
    {
        var levels = FeatureLevels.Create().Value;

        var result = levels.HasAny();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasNoneAndSome_ThenReturnsFalse()
    {
        var levels = FeatureLevels.Create().Value;
        levels = levels.Add(PlatformFeatureLevels.Basic.Name).Value;

        var result = levels.HasNone();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasNoneAndNone_ThenReturnsTrue()
    {
        var levels = FeatureLevels.Create().Value;

        var result = levels.HasNone();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasLevelAndInvalidName_ThenReturnsFalse()
    {
        var levels = FeatureLevels.Create().Value;

        var result = levels.HasLevel("anunknownlevel");

        result.Should().BeFalse();
    }

#if TESTINGONLY
    [Fact]
    public void WhenHasHasLevelAndNoMatch_ThenReturnsFalse()
    {
        var levels = FeatureLevels.Create().Value;
        levels = levels.Add(PlatformFeatureLevels.Basic.Name).Value;

        var result = levels.HasLevel(PlatformFeatureLevels.TestingOnlyLevel.Name);

        result.Should().BeFalse();
    }
#endif

    [Fact]
    public void WhenHasHasLevelAndMatching_ThenReturnsTrue()
    {
        var levels = FeatureLevels.Create().Value;
        levels = levels.Add(PlatformFeatureLevels.Basic.Name).Value;

        var result = levels.HasLevel(PlatformFeatureLevels.Basic.Name);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenRemoveAndInvalidName_ThenDoesNotRemove()
    {
        var levels = FeatureLevels.Create().Value;
        levels = levels.Add(PlatformFeatureLevels.Basic.Name).Value;

        var result = levels.Remove("anunknownlevel");

        result.Should().Be(levels);
    }

#if TESTINGONLY
    [Fact]
    public void WhenRemoveAndNoMatch_ThenDoesNotRemove()
    {
        var levels = FeatureLevels.Create().Value;
        levels = levels.Add(PlatformFeatureLevels.Basic.Name).Value;

        var result = levels.Remove(PlatformFeatureLevels.TestingOnlyLevel.Name);

        result.Should().Be(levels);
    }
#endif

    [Fact]
    public void WhenRemoveAndMatches_ThenRemoves()
    {
        var levels = FeatureLevels.Create().Value;
        levels = levels.Add(PlatformFeatureLevels.Basic.Name).Value;

        var result = levels.Remove(PlatformFeatureLevels.Basic.Name);

        result.Items.Should().BeEmpty();
    }

#if TESTINGONLY
    [Fact]
    public void WhenToList_ThenReturnsStringList()
    {
        var levels = FeatureLevels.Create().Value;
        levels = levels.Add(PlatformFeatureLevels.Basic.Name).Value;
        levels = levels.Add(PlatformFeatureLevels.TestingOnlyLevel.Name).Value;

        var result = levels.ToList();

        result.Should().ContainInOrder(PlatformFeatureLevels.Basic.Name, PlatformFeatureLevels.TestingOnlyLevel.Name);
    }
#endif
}