using Common;
using Domain.Interfaces.Authorization;
using FluentAssertions;
using UnitTesting.Common;
using Xunit;

namespace Domain.Shared.UnitTests;

[Trait("Category", "Unit")]
public class FeatureSpec
{
    [Fact]
    public void WhenCreateWithEmpty_ThenReturnsError()
    {
        var result = Feature.Create(new FeatureLevel(string.Empty));

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreateWithInvalidName_ThenReturnsError()
    {
        var result = Feature.Create(new FeatureLevel("^aninvalidname^"));

        result.Should().BeError(ErrorCode.Validation, Resources.Features_InvalidFeature);
    }

    [Fact]
    public void WhenCreateWithAnUnknownName_ThenReturnsValue()
    {
        var result = Feature.Create(new FeatureLevel("anunknownfeature"));

        result.Should().BeSuccess();
        result.Value.Identifier.Should().Be("anunknownfeature");
    }

    [Fact]
    public void WhenCreateWithAKnownName_ThenReturnsValue()
    {
        var result = Feature.Create(PlatformFeatures.Basic);

        result.Should().BeSuccess();
        result.Value.Identifier.Should().Be(PlatformFeatures.Basic.Name);
    }
}

[Trait("Category", "Unit")]
public class FeaturesSpec
{
    [Fact]
    public void WhenCreate_ThenReturnsError()
    {
        var result = Features.Empty;

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenCreateWithSingleEmpty_ThenReturnsError()
    {
        var result = Features.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreateWithSingle_ThenReturnsValue()
    {
        var result = Features.Create(PlatformFeatures.Basic);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(Feature.Create(PlatformFeatures.Basic).Value);
    }

#if TESTINGONLY
    [Fact]
    public void WhenCreateWithFeatureLevel_ThenReturnsValue()
    {
        var result = Features.Create(PlatformFeatures.TestingOnlySuperUser);

        result.Should().BeSuccess();
        result.Value.Items.Should()
            .OnlyContain(feat => feat == Feature.Create(PlatformFeatures.TestingOnlySuperUser).Value);
    }
#endif

    [Fact]
    public void WhenCreateWithListContainingInvalidItem_ThenReturnsError()
    {
        var result = Features.Create(PlatformFeatures.Basic.Name, string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

#if TESTINGONLY
    [Fact]
    public void WhenCreateWithListContainingValidItems_ThenReturnsValue()
    {
        var result = Features.Create(PlatformFeatures.Basic, PlatformFeatures.TestingOnly);

        result.Should().BeSuccess();
        result.Value.Items.Count.Should().Be(2);
        result.Value.Items.Should().ContainInOrder(Feature.Create(PlatformFeatures.Basic).Value,
            Feature.Create(PlatformFeatures.TestingOnly).Value);
    }
#endif

#if TESTINGONLY
    [Fact]
    public void WhenCreateWithListContainingParent_ThenReturnsNormalizedValue()
    {
        var result = Features.Create(PlatformFeatures.Paid3, PlatformFeatures.Paid2, PlatformFeatures.PaidTrial,
            PlatformFeatures.TestingOnly);

        result.Should().BeSuccess();
        result.Value.Items.Count.Should().Be(2);
        result.Value.Items.Should().ContainInOrder(Feature.Create(PlatformFeatures.Paid3).Value,
            Feature.Create(PlatformFeatures.TestingOnly).Value);
    }
#endif

    [Fact]
    public void WhenAddStringWithUnknownName_ThenAddsFeature()
    {
        var features = Features.Empty;

        var result = features.Add("anunknownfeature");

        result.Should().BeSuccess();
        result.Value.Items.Should().OnlyContain(feat => feat.Identifier == "anunknownfeature");
    }

    [Fact]
    public void WhenAddStringWithKnownName_ThenAddsFeature()
    {
        var features = Features.Empty;

        var result = features.Add(PlatformFeatures.Basic.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().OnlyContain(feat => feat.Identifier == PlatformFeatures.Basic.Name);
    }

#if TESTINGONLY
    [Fact]
    public void WhenAddFeatureLevel_ThenReturnsValue()
    {
        var features = Features.Create(PlatformFeatures.Basic).Value;

        var result = features.Add(PlatformFeatures.TestingOnlySuperUser);

        result.Should().BeSuccess();
        result.Value.Items.Count.Should().Be(2);
        result.Value.Items.Should().ContainInOrder(
            Feature.Create(PlatformFeatures.Basic).Value,
            Feature.Create(PlatformFeatures.TestingOnlySuperUser).Value);
    }
#endif

    [Fact]
    public void WhenAddParentFeatureLevel_ThenReturnsNormalizedValue()
    {
        var features = Features.Create(PlatformFeatures.Basic).Value;

        var result = features.Add(PlatformFeatures.PaidTrial);

        result.Should().BeSuccess();
        result.Value.Items.Count.Should().Be(1);
        result.Value.Items.Should().ContainInOrder(
            Feature.Create(PlatformFeatures.PaidTrial).Value);
    }

    [Fact]
    public void WhenAddChildFeatureLevel_ThenReturnsNormalizedValue()
    {
        var features = Features.Create(PlatformFeatures.PaidTrial).Value;

        var result = features.Add(PlatformFeatures.Basic);

        result.Should().BeSuccess();
        result.Value.Items.Count.Should().Be(1);
        result.Value.Items.Should().ContainInOrder(
            Feature.Create(PlatformFeatures.PaidTrial).Value);
    }

    [Fact]
    public void WhenAddFeatureAndExists_ThenDoesNotAdd()
    {
        var features = Features.Empty;
        features.Add(PlatformFeatures.Basic.Name);

        var result = features.Add(PlatformFeatures.Basic.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().OnlyContain(feat => feat.Identifier == PlatformFeatures.Basic.Name);
    }

#if TESTINGONLY
    [Fact]
    public void WhenAddFeatureAndNotExists_ThenAdds()
    {
        var features = Features.Empty;
        features = features.Add(PlatformFeatures.Basic).Value;

        var result = features.Add(PlatformFeatures.TestingOnly);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(Feature.Create(PlatformFeatures.Basic).Value,
            Feature.Create(PlatformFeatures.TestingOnly).Value);
    }
#endif

    [Fact]
    public void WhenClear_ThenRemovesAllItems()
    {
        var features = Features.Empty;
        features = features.Add(PlatformFeatures.Basic).Value;

        var result = features.Clear();

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenHasAnyAndSome_ThenReturnsTrue()
    {
        var features = Features.Empty;
        features = features.Add(PlatformFeatures.Basic).Value;

        var result = features.HasAny();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasAnyAndNone_ThenReturnsFalse()
    {
        var features = Features.Empty;

        var result = features.HasAny();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasNoneAndSome_ThenReturnsFalse()
    {
        var features = Features.Empty;
        features = features.Add(PlatformFeatures.Basic).Value;

        var result = features.HasNone();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasNoneAndNone_ThenReturnsTrue()
    {
        var features = Features.Empty;

        var result = features.HasNone();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasLevelAndInvalidName_ThenReturnsFalse()
    {
        var features = Features.Empty;

        var result = features.HasFeature(new FeatureLevel("anunknownfeature"));

        result.Should().BeFalse();
    }

#if TESTINGONLY
    [Fact]
    public void WhenHasHasLevelAndNoMatch_ThenReturnsFalse()
    {
        var features = Features.Empty;
        features = features.Add(PlatformFeatures.Basic).Value;

        var result = features.HasFeature(PlatformFeatures.TestingOnly);

        result.Should().BeFalse();
    }
#endif

    [Fact]
    public void WhenHasFeatureAndHasSameFeature_ThenReturnsTrue()
    {
        var features = Features.Empty;
        features = features.Add(PlatformFeatures.Basic).Value;

        var result = features.HasFeature(PlatformFeatures.Basic);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasFeatureAndHasParentFeature_ThenReturnsFalse()
    {
        var features = Features.Empty;
        features = features.Add(PlatformFeatures.Basic).Value;

        var result = features.HasFeature(PlatformFeatures.PaidTrial);

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasFeatureAndHasChildFeature_ThenReturnsTrue()
    {
        var features = Features.Empty;
        features = features.Add(PlatformFeatures.PaidTrial).Value;

        var result = features.HasFeature(PlatformFeatures.Basic);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenRemoveAndInvalidName_ThenDoesNotRemove()
    {
        var features = Features.Empty;
        features = features.Add(PlatformFeatures.Basic).Value;

        var result = features.Remove("anunknownfeature");

        result.Should().Be(features);
    }

#if TESTINGONLY
    [Fact]
    public void WhenRemoveAndNoMatch_ThenDoesNotRemove()
    {
        var features = Features.Empty;
        features = features.Add(PlatformFeatures.Basic).Value;

        var result = features.Remove(PlatformFeatures.TestingOnly.Name);

        result.Should().Be(features);
    }
#endif

    [Fact]
    public void WhenRemoveAndMatches_ThenRemoves()
    {
        var features = Features.Empty;
        features = features.Add(PlatformFeatures.Basic).Value;

        var result = features.Remove(PlatformFeatures.Basic);

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenRemoveChildAndHasParent_ThenLeavesParent()
    {
        var features = Features.Empty;
        features = features.Add(PlatformFeatures.PaidTrial).Value;

        var result = features.Remove(PlatformFeatures.Basic);

        result.Items.Should().ContainInOrder(Feature.Create(PlatformFeatures.PaidTrial).Value);
    }

    [Fact]
    public void WhenRemoveParentAndHasDescendants_ThenLeavesDescendants()
    {
        var features = Features.Empty;
        features = features.Add(PlatformFeatures.PaidTrial).Value;

        var result = features.Remove(PlatformFeatures.PaidTrial);

        result.Items.Should().ContainInOrder(Feature.Create(PlatformFeatures.Basic).Value);
    }

    [Fact]
    public void WhenRemoveParentAndNoDescendants_ThenRemovesParent()
    {
        var features = Features.Empty;
        features = features.Add(PlatformFeatures.Basic).Value;

        var result = features.Remove(PlatformFeatures.Basic);

        result.Items.Should().BeEmpty();
    }

#if TESTINGONLY
    [Fact]
    public void WhenDenormalize_ThenReturnsDenormalizedList()
    {
        var features = Features.Empty;
        features = features.Add(PlatformFeatures.PaidTrial).Value;
        features = features.Add(PlatformFeatures.TestingOnlySuperUser).Value;

        var result = features.Denormalize();

        result.Count.Should().Be(4);
        result.Should().ContainInOrder(PlatformFeatures.PaidTrial.Name, PlatformFeatures.Basic.Name,
            PlatformFeatures.TestingOnlySuperUser.Name, PlatformFeatures.TestingOnly.Name);
    }
#endif
}