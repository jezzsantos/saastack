namespace Domain.Interfaces.Authorization;

/// <summary>
///     Defines an authorization level
/// </summary>
public interface IHierarchicalLevel<TLevel>
{
    IReadOnlyList<TLevel> Children { get; }

    public string Name { get; }

    IReadOnlyList<string> AllDescendantNames();

    bool HasDescendant(TLevel level);
}