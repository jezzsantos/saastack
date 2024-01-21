namespace Domain.Interfaces.Authorization;

/// <summary>
///     Defines a role level
///     Role levels can be hierarchical. A parent role automatically grants access to any child role
/// </summary>
public class RoleLevel : HierarchicalLevelBase<RoleLevel>
{
    public RoleLevel(string name, params RoleLevel[] children) : base(name,
        children.Select(x => (HierarchicalLevelBase<RoleLevel>)x).ToArray())
    {
    }
}