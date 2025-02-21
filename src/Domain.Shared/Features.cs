using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Extensions;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Shared;

/// <summary>
///     Defines a collection of normalized <see cref="Feature" />.
///     Since we only store the Name of the <see cref="FeatureLevel" /> we need to maintain a normalized collection
///     of <see cref="Feature" />.
/// </summary>
public sealed class Features : SingleValueObjectBase<Features, List<Feature>>
{
    public static readonly Features Empty = new();

    public static Result<Features, Error> Create(string feature)
    {
        return Create(feature.ToFeatureLevel());
    }

    public static Result<Features, Error> Create(FeatureLevel feature)
    {
        return Create([feature]);
    }

    public static Result<Features, Error> Create(params string[] features)
    {
        return Create(features.Select(feature => feature.ToFeatureLevel()).ToArray());
    }

    public static Result<Features, Error> Create(params FeatureLevel[] features)
    {
        var normalized = features.Normalize();
        var list = new List<Feature>();
        foreach (var feature in normalized)
        {
            var feat = Feature.Create(feature);
            if (feat.IsFailure)
            {
                return feat.Error;
            }

            list.Add(feat.Value);
        }

        return new Features(list);
    }

    private Features() : base([])
    {
    }

    private Features(IEnumerable<Feature> features) : base(features.ToList())
    {
    }

    public List<Feature> Items => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<Features> Rehydrate()
    {
        return (property, container) =>
        {
            var items = RehydrateToList(property, true, true);
            var features = items.Select(item => Feature.Rehydrate()(item!, container));
            return new Features(features);
        };
    }

    public Result<Features, Error> Add(string feature)
    {
        var featureLevel = Feature.Create(feature.ToFeatureLevel());
        if (featureLevel.IsFailure)
        {
            return featureLevel.Error;
        }

        return Add(featureLevel.Value);
    }

    public Result<Features, Error> Add(FeatureLevel feature)
    {
        var featureLevel = Feature.Create(feature);
        if (featureLevel.IsFailure)
        {
            return featureLevel.Error;
        }

        return Add(featureLevel.Value);
    }

#pragma warning disable CA1822
    public Features Clear()
#pragma warning restore CA1822
    {
        return Empty;
    }

    [SkipImmutabilityCheck]
    public List<string> Denormalize()
    {
        return Items
            .Select(feat => feat.AsLevel())
            .ToArray()
            .Denormalize()
            .ToList();
    }

    [SkipImmutabilityCheck]
    public bool HasAny()
    {
        return Value.HasAny();
    }

    [SkipImmutabilityCheck]
    public bool HasFeature(Feature feature)
    {
        var denormalized = Denormalize();
        return denormalized.ContainsIgnoreCase(feature.Identifier);
    }

    [SkipImmutabilityCheck]
    public bool HasFeature(FeatureLevel feature)
    {
        var feat = Feature.Create(feature);
        if (feat.IsFailure)
        {
            return false;
        }

        return HasFeature(feat.Value);
    }

    [SkipImmutabilityCheck]
    public bool HasNone()
    {
        return Value.HasNone();
    }

    public Features Remove(string feature)
    {
        return Remove(feature.ToFeatureLevel());
    }

    public Features Remove(FeatureLevel feature)
    {
        var feat = Feature.Create(feature);
        if (feat.IsFailure)
        {
            return this;
        }

        return Remove(feat.Value);
    }

    private Result<Features, Error> Add(Feature feature)
    {
        if (HasFeature(feature))
        {
            return new Features(Value);
        }

        var features = Value
            .Select(val => val.AsLevel())
            .ToArray()
            .Merge(feature.AsLevel())
            .Select(level => Feature.Create(level).Value);

        return new Features(features);
    }

    private Features Remove(Feature feature)
    {
        if (!HasFeature(feature))
        {
            return new Features(Value);
        }

        var features = Value
            .Select(feat => feat.AsLevel())
            .ToArray()
            .UnMerge(feature.AsLevel())
            .Select(level => Feature.Create(level).Value);

        return new Features(features);
    }
}