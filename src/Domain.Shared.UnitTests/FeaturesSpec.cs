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
        var result = Feature.Create(string.Empty);

        result.Should().BeError(ErrorCode.Validation);
    }

    [Fact]
    public void WhenCreateWithInvalidName_ThenReturnsError()
    {
        var result = Feature.Create("^aninvalidname^");

        result.Should().BeError(ErrorCode.Validation, Resources.Features_InvalidFeature);
    }

    [Fact]
    public void WhenCreateWithAnUnknownName_ThenReturnsValue()
    {
        var result = Feature.Create("anunknownfeature");

        result.Should().BeSuccess();
        result.Value.Identifier.Should().Be("anunknownfeature");
    }

    [Fact]
    public void WhenCreateWithAKnownName_ThenReturnsValue()
    {
        var result = Feature.Create(PlatformFeatures.Basic.Name);

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
        var result = Features.Create();

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
        var result = Features.Create(PlatformFeatures.Basic.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(Feature.Create(PlatformFeatures.Basic.Name).Value);
    }

#if TESTINGONLY
    [Fact]
    public void WhenCreateWithFeatureLevel_ThenReturnsValue()
    {
        var result = Features.Create(PlatformFeatures.TestingOnlySuperUser);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(Feature.Create(
                PlatformFeatures.TestingOnlySuperUser.Name).Value,
            Feature.Create(PlatformFeatures.TestingOnly.Name).Value);
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
        var result = Features.Create(PlatformFeatures.Basic.Name, PlatformFeatures.TestingOnly.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(Feature.Create(PlatformFeatures.Basic.Name).Value,
            Feature.Create(PlatformFeatures.TestingOnly.Name).Value);
    }
#endif

    [Fact]
    public void WhenAddStringWithUnknownName_ThenAddsFeature()
    {
        var features = Features.Create();

        var result = features.Add("anunknownfeature");

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainSingle(feat => feat.Identifier == "anunknownfeature");
    }

    [Fact]
    public void WhenAddStringWithKnownName_ThenAddsFeature()
    {
        var features = Features.Create();

        var result = features.Add(PlatformFeatures.Basic.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainSingle(feat => feat.Identifier == PlatformFeatures.Basic.Name);
    }

#if TESTINGONLY
    [Fact]
    public void WhenAddFeatureLevel_ThenReturnsValue()
    {
        var features = Features.Create(PlatformFeatures.Basic).Value;

        var result = features.Add(PlatformFeatures.TestingOnlySuperUser);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(
            Feature.Create(PlatformFeatures.Basic.Name).Value,
            Feature.Create(PlatformFeatures.TestingOnlySuperUser.Name).Value,
            Feature.Create(PlatformFeatures.TestingOnly.Name).Value);
    }
#endif

    [Fact]
    public void WhenAddFeatureAndExists_ThenDoesNotAdd()
    {
        var features = Features.Create();
        features.Add(PlatformFeatures.Basic.Name);

        var result = features.Add(PlatformFeatures.Basic.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainSingle(feat => feat.Identifier == PlatformFeatures.Basic.Name);
    }

#if TESTINGONLY
    [Fact]
    public void WhenAddFeatureAndNotExists_ThenAdds()
    {
        var features = Features.Create();
        features = features.Add(PlatformFeatures.Basic.Name).Value;

        var result = features.Add(PlatformFeatures.TestingOnly.Name);

        result.Should().BeSuccess();
        result.Value.Items.Should().ContainInOrder(Feature.Create(PlatformFeatures.Basic.Name).Value,
            Feature.Create(PlatformFeatures.TestingOnly.Name).Value);
    }
#endif

    [Fact]
    public void WhenClear_ThenRemovesAllItems()
    {
        var features = Features.Create();
        features = features.Add(PlatformFeatures.Basic.Name).Value;

        var result = features.Clear();

        result.Items.Should().BeEmpty();
    }

    [Fact]
    public void WhenHasAnyAndSome_ThenReturnsTrue()
    {
        var features = Features.Create();
        features = features.Add(PlatformFeatures.Basic.Name).Value;

        var result = features.HasAny();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasAnyAndNone_ThenReturnsFalse()
    {
        var features = Features.Create();

        var result = features.HasAny();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasNoneAndSome_ThenReturnsFalse()
    {
        var features = Features.Create();
        features = features.Add(PlatformFeatures.Basic.Name).Value;

        var result = features.HasNone();

        result.Should().BeFalse();
    }

    [Fact]
    public void WhenHasNoneAndNone_ThenReturnsTrue()
    {
        var features = Features.Create();

        var result = features.HasNone();

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenHasLevelAndInvalidName_ThenReturnsFalse()
    {
        var features = Features.Create();

        var result = features.HasFeature("anunknownfeature");

        result.Should().BeFalse();
    }

#if TESTINGONLY
    [Fact]
    public void WhenHasHasLevelAndNoMatch_ThenReturnsFalse()
    {
        var features = Features.Create();
        features = features.Add(PlatformFeatures.Basic.Name).Value;

        var result = features.HasFeature(PlatformFeatures.TestingOnly.Name);

        result.Should().BeFalse();
    }
#endif

    [Fact]
    public void WhenHasHasLevelAndMatching_ThenReturnsTrue()
    {
        var features = Features.Create();
        features = features.Add(PlatformFeatures.Basic.Name).Value;

        var result = features.HasFeature(PlatformFeatures.Basic.Name);

        result.Should().BeTrue();
    }

    [Fact]
    public void WhenRemoveAndInvalidName_ThenDoesNotRemove()
    {
        var features = Features.Create();
        features = features.Add(PlatformFeatures.Basic.Name).Value;

        var result = features.Remove("anunknownfeature");

        result.Should().Be(features);
    }

#if TESTINGONLY
    [Fact]
    public void WhenRemoveAndNoMatch_ThenDoesNotRemove()
    {
        var features = Features.Create();
        features = features.Add(PlatformFeatures.Basic.Name).Value;

        var result = features.Remove(PlatformFeatures.TestingOnly.Name);

        result.Should().Be(features);
    }
#endif

    [Fact]
    public void WhenRemoveAndMatches_ThenRemoves()
    {
        var features = Features.Create();
        features = features.Add(PlatformFeatures.Basic.Name).Value;

        var result = features.Remove(PlatformFeatures.Basic.Name);

        result.Items.Should().BeEmpty();
    }

#if TESTINGONLY
    [Fact]
    public void WhenToList_ThenReturnsStringList()
    {
        var features = Features.Create();
        features = features.Add(PlatformFeatures.Basic.Name).Value;
        features = features.Add(PlatformFeatures.TestingOnly.Name).Value;

        var result = features.ToList();

        result.Should().ContainInOrder(PlatformFeatures.Basic.Name, PlatformFeatures.TestingOnly.Name);
    }
#endif
}