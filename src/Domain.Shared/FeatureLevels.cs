using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Authorization;
using Domain.Interfaces.Validations;
using Domain.Interfaces.ValueObjects;

namespace Domain.Shared;

public class FeatureLevel : SingleValueObjectBase<FeatureLevel, string>
{
    public static Result<FeatureLevel, Error> Create(string identifier)
    {
        if (identifier.IsNotValuedParameter(nameof(identifier), out var error1))
        {
            return error1;
        }

        if (identifier.IsInvalidParameter(CommonValidations.FeatureLevel, nameof(identifier),
                Resources.FeatureLevels_InvalidFeatureLevel, out var error2))
        {
            return error2;
        }

        if (identifier.IsInvalidParameter(
                lvl => PlatformFeatureLevels.IsPlatformAssignableFeatureLevel(lvl)
                       || MemberFeatureLevels.IsMemberAssignableFeatureLevel(lvl), nameof(identifier),
                Resources.FeatureLevels_InvalidFeatureLevel, out var error3))
        {
            return error3;
        }

        return new FeatureLevel(identifier);
    }

    private FeatureLevel(string identifier) : base(identifier.ToLowerInvariant())
    {
    }

    public string Identifier => Value;

    public static ValueObjectFactory<FeatureLevel> Rehydrate()
    {
        return (property, _) =>
        {
            var parts = RehydrateToList(property, true);
            return new FeatureLevel(parts[0]!);
        };
    }
}

public class FeatureLevels : SingleValueObjectBase<FeatureLevels, List<FeatureLevel>>
{
    public static readonly FeatureLevels Empty = new();

    public static Result<FeatureLevels, Error> Create()
    {
        return new FeatureLevels();
    }

    public static Result<FeatureLevels, Error> Create(string level)
    {
        var lvl = FeatureLevel.Create(level);
        if (!lvl.IsSuccessful)
        {
            return lvl.Error;
        }

        return new FeatureLevels(lvl.Value);
    }

    public static Result<FeatureLevels, Error> Create(IEnumerable<string> levels)
    {
        var list = new List<FeatureLevel>();
        foreach (var level in levels)
        {
            var lvl = FeatureLevel.Create(level);
            if (!lvl.IsSuccessful)
            {
                return lvl.Error;
            }

            list.Add(lvl.Value);
        }

        return new FeatureLevels(list);
    }

    private FeatureLevels() : base(new List<FeatureLevel>())
    {
    }

    private FeatureLevels(FeatureLevel level) : base(new List<FeatureLevel> { level })
    {
    }

    private FeatureLevels(IEnumerable<FeatureLevel> levels) : base(levels.ToList())
    {
    }

    public List<FeatureLevel> Items => Value;

    public static ValueObjectFactory<FeatureLevels> Rehydrate()
    {
        return (property, container) =>
        {
            var items = RehydrateToList(property, true, true);
            var levels = items.Select(item => FeatureLevel.Rehydrate()(item!, container));
            return new FeatureLevels(levels);
        };
    }

    public Result<FeatureLevels, Error> Add(string level)
    {
        var featureLevel = FeatureLevel.Create(level);
        if (!featureLevel.IsSuccessful)
        {
            return featureLevel.Error;
        }

        return Add(featureLevel.Value);
    }

    public Result<FeatureLevels, Error> Add(FeatureLevel level)
    {
        if (!Value.Contains(level))
        {
            var newValues = Value.Concat(new[] { level });
            return new FeatureLevels(newValues);
        }

        return new FeatureLevels(Value);
    }

#pragma warning disable CA1822
    public FeatureLevels Clear()
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
    public bool HasLevel(string level)
    {
        var lvl = FeatureLevel.Create(level);
        if (!lvl.IsSuccessful)
        {
            return false;
        }

        return HasLevel(lvl.Value);
    }

    [SkipImmutabilityCheck]
    public bool HasLevel(FeatureLevel featureLevel)
    {
        return Value.ToList().Select(lvl => lvl.Identifier).ContainsIgnoreCase(featureLevel);
    }

    [SkipImmutabilityCheck]
    public bool HasNone()
    {
        return Value.HasNone();
    }

    public FeatureLevels Remove(string level)
    {
        var lvl = FeatureLevel.Create(level);
        if (!lvl.IsSuccessful)
        {
            return this;
        }

        return Remove(lvl.Value);
    }

    public FeatureLevels Remove(FeatureLevel featureLevel)
    {
        if (Value.Contains(featureLevel))
        {
            var remaining = Value
                .Where(lvl => !lvl.Equals(featureLevel))
                .ToList();

            return new FeatureLevels(remaining);
        }

        return new FeatureLevels(Value);
    }

    [SkipImmutabilityCheck]
    public List<string> ToList()
    {
        return Items.Select(lvl => lvl.Identifier).ToList();
    }
}