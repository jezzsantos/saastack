namespace Domain.Interfaces.Authorization;

/// <summary>
///     Defines a feature level.
///     Feature levels can be hierarchical. A parent feature automatically grants access to any child feature
/// </summary>
public sealed class FeatureLevel : HierarchicalLevelBase<FeatureLevel>
{
    public FeatureLevel(string name, params FeatureLevel[] children) : base(name,
        children.Select(x => (HierarchicalLevelBase<FeatureLevel>)x).ToArray())
    {
    }
}