namespace Domain.Interfaces.Authorization;

/// <summary>
///     Defines a feature level
/// </summary>
public sealed class FeatureLevel
{
    public FeatureLevel(string name, params FeatureLevel[] childLevels)
    {
        Name = name;
        ChildLevels = childLevels;
    }

    public IReadOnlyList<FeatureLevel> ChildLevels { get; }

    public string Name { get; }
}