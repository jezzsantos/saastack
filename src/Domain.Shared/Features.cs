using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.ValueObjects;

namespace Domain.Shared;

public sealed class Features : SingleValueObjectBase<Features, List<Feature>>
{
    public static readonly Features Empty = new();

    public static Features Create()
    {
        return new Features();
    }

    public static Result<Features, Error> Create(string feature)
    {
        var feat = Feature.Create(feature);
        if (!feat.IsSuccessful)
        {
            return feat.Error;
        }

        return new Features(feat.Value);
    }

    public static Result<Features, Error> Create(FeatureLevel feature)
    {
        var allLevels = feature.AllDescendantNames().ToArray();
        return Create(allLevels);
    }

    public static Result<Features, Error> Create(params string[] features)
    {
        var list = new List<Feature>();
        foreach (var feature in features)
        {
            var feat = Feature.Create(feature);
            if (!feat.IsSuccessful)
            {
                return feat.Error;
            }

            list.Add(feat.Value);
        }

        return new Features(list);
    }

    private Features() : base(new List<Feature>())
    {
    }

    private Features(Feature feature) : base(new List<Feature> { feature })
    {
    }

    private Features(IEnumerable<Feature> features) : base(features.ToList())
    {
    }

    public List<Feature> Items => Value;

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
        var featureLevel = Feature.Create(feature);
        if (!featureLevel.IsSuccessful)
        {
            return featureLevel.Error;
        }

        return Add(featureLevel.Value);
    }

    public Result<Features, Error> Add(FeatureLevel feature)
    {
        var features = new Features(Value.ToArray());
        var allLevels = feature.AllDescendantNames().ToArray();
        foreach (var level in allLevels)
        {
            var added = features.Add(level);
            if (!added.IsSuccessful)
            {
                return added.Error;
            }

            features = added.Value;
        }

        return features;
    }

    public Result<Features, Error> Add(Feature feature)
    {
        if (!Value.Contains(feature))
        {
            var newValues = Value.Concat(new[] { feature });
            return new Features(newValues);
        }

        return new Features(Value);
    }

#pragma warning disable CA1822
    public Features Clear()
#pragma warning restore CA1822
    {
        return Empty;
    }

    [SkipImmutabilityCheck]
    public bool HasAny()
    {
        return Value.HasAny();
    }

    [SkipImmutabilityCheck]
    public bool HasFeature(string feature)
    {
        var feat = Feature.Create(feature);
        if (!feat.IsSuccessful)
        {
            return false;
        }

        return HasFeature(feat.Value);
    }

    [SkipImmutabilityCheck]
    public bool HasFeature(Feature feature)
    {
        return Value.ToList().Select(feat => feat.Identifier).ContainsIgnoreCase(feature);
    }

    [SkipImmutabilityCheck]
    public bool HasFeature(FeatureLevel feature)
    {
        return HasFeature(feature.Name);
    }

    [SkipImmutabilityCheck]
    public bool HasNone()
    {
        return Value.HasNone();
    }

    public Features Remove(string feature)
    {
        var feat = Feature.Create(feature);
        if (!feat.IsSuccessful)
        {
            return this;
        }

        return Remove(feat.Value);
    }

    public Features Remove(Feature feature)
    {
        if (Value.Contains(feature))
        {
            var remaining = Value
                .Where(feat => !feat.Equals(feature))
                .ToList();

            return new Features(remaining);
        }

        return new Features(Value);
    }

    [SkipImmutabilityCheck]
    public List<string> ToList()
    {
        return Items.Select(feat => feat.Identifier).ToList();
    }
}