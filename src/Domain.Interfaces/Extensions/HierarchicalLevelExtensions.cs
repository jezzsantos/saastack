using Common.Extensions;
using Domain.Interfaces.Authorization;

namespace Domain.Interfaces.Extensions;

public static class HierarchicalLevelExtensions
{
    /// <summary>
    ///     De-normalizes (expands) set of levels from all levels.
    ///     i.e. all descendant levels are expanded out.
    /// </summary>
    public static string[] Denormalize<TLevel>(this TLevel[]? levels)
        where TLevel : HierarchicalLevelBase<TLevel>
    {
        if (levels.NotExists())
        {
            return [];
        }

        if (levels.HasNone())
        {
            return [];
        }

        return levels
            .SelectMany(x => x.AllDescendantNames())
            .Distinct()
            .ToArray();
    }

    /// <summary>
    ///     Adds the specified <see cref="level" /> to the normalized <see cref="levels" />.
    ///     Note: In the case where a descendant is merged, where an ancestor already exists, the ancestor is kept,
    ///     and the descendant is discarded.
    /// </summary>
    public static TLevel[] Merge<TLevel>(this TLevel[]? levels, TLevel level)
        where TLevel : HierarchicalLevelBase<TLevel>
    {
        if (levels.NotExists())
        {
            return [level];
        }

        if (levels.Contains(level))
        {
            return levels;
        }

        return levels
            .Normalize()
            .ToArray()
            .Concat([level])
            .ToArray()
            .Normalize();
    }

    /// <summary>
    ///     Normalizes (reduces) set of levels to the ancestor levels only.
    ///     i.e. any redundant descendant levels are removed.
    /// </summary>
    public static TLevel[] Normalize<TLevel>(this TLevel[]? levels)
        where TLevel : HierarchicalLevelBase<TLevel>
    {
        if (levels.NotExists())
        {
            return [];
        }

        if (levels.HasNone())
        {
            return [];
        }

        if (levels.Length == 1)
        {
            return [levels[0]];
        }

        var uniqueLevels = levels
            .Distinct()
            .ToArray();

        var results = new List<TLevel>();
        foreach (var level in uniqueLevels)
        {
            var others = levels.Except([level]);
            if (!IsDescendantOfOthers(level, others))
            {
                results.Add(level);
            }
        }

        return results.ToArray();

        static bool IsDescendantOfOthers(TLevel level, IEnumerable<TLevel> others)
        {
            foreach (var other in others)
            {
                if (other.HasDescendant(level))
                {
                    return true;
                }
            }

            return false;
        }
    }

    /// <summary>
    ///     Removes the specified <see cref="level" /> from the normalized <see cref="levels" />.
    ///     Note: In the case where an ancestor is unmerged, where the ancestor had children, the children are kept,
    ///     and the ancestor is discarded.
    /// </summary>
    public static TLevel[] UnMerge<TLevel>(this TLevel[]? levels, TLevel level)
        where TLevel : HierarchicalLevelBase<TLevel>
    {
        if (levels.NotExists())
        {
            return [];
        }

        if (levels.HasNone())
        {
            return [];
        }

        var children = level.Children
            .Where(lvl => !levels.Contains(lvl))
            .OfType<TLevel>()
            .ToArray();

        return levels
            .Normalize()
            .Except([level])
            .ToArray()
            .Concat(children)
            .ToArray()
            .Normalize();
    }
}