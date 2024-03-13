namespace Domain.Interfaces.Authorization;

/// <summary>
///     Provides a base class for managing levels
/// </summary>
public abstract class HierarchicalLevelBase<TLevel> : IHierarchicalLevel<HierarchicalLevelBase<TLevel>>
{
    protected HierarchicalLevelBase(string name, HierarchicalLevelBase<TLevel>[] children)
    {
        Name = name;
        Children = children;
    }

    public IReadOnlyList<HierarchicalLevelBase<TLevel>> Children { get; }

    public string Name { get; }

    /// <summary>
    ///     Returns all the names of all the descendants in the hierarchy, including the name of the this level
    /// </summary>
    public IReadOnlyList<string> AllDescendantNames()
    {
        return GetDescendantNames(this, false);
    }

    /// <summary>
    ///     Whether the specified <see cref="level" /> exists in this hierarchy
    /// </summary>
    public bool HasDescendant(HierarchicalLevelBase<TLevel> level)
    {
        return FindDescendant(this, lvl => lvl == level);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || (obj is HierarchicalLevelBase<TLevel> other && Equals(other));
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public static bool operator ==(HierarchicalLevelBase<TLevel>? left, HierarchicalLevelBase<TLevel>? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(HierarchicalLevelBase<TLevel>? left, HierarchicalLevelBase<TLevel>? right)
    {
        return !Equals(left, right);
    }

    public override string ToString()
    {
        return Name;
    }

    private static List<string> GetDescendantNames(HierarchicalLevelBase<TLevel> parent, bool skipParent)
    {
        var names = new List<string>();
        ScanDescendantsBreadthFirst(parent, node =>
        {
            names.Add(node.Name);
            return true; // continue enumerating
        }, skipParent);

        return names;
    }

    private static bool FindDescendant(HierarchicalLevelBase<TLevel> parent,
        Predicate<HierarchicalLevelBase<TLevel>> match)
    {
        var matched = false;
        ScanDescendantsBreadthFirst(parent, node =>
        {
            matched = match(node);
            if (matched)
            {
                return false; //stop enumerating
            }

            return true; // continue enumerating
        }, true);

        return matched;
    }

    /// <summary>
    ///     Scans a tree, breadth first before decending, and continues if the specified <see cref="accumulate" />
    ///     function returns true, stops otherwise
    /// </summary>
    private static void ScanDescendantsBreadthFirst(HierarchicalLevelBase<TLevel> parent,
        Func<HierarchicalLevelBase<TLevel>, bool> accumulate, bool skipParent)
    {
        var queue = new Queue<HierarchicalLevelBase<TLevel>>();
        if (skipParent)
        {
            foreach (var child in parent.Children)
            {
                queue.Enqueue(child);
            }
        }
        else
        {
            queue.Enqueue(parent);
        }

        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (!accumulate(node))
            {
                return;
            }

            foreach (var child in node.Children)
            {
                queue.Enqueue(child);
            }
        }
    }

    private bool Equals(HierarchicalLevelBase<TLevel> other)
    {
        return Name == other.Name
               && AreChildrenEqual(Children, other.Children);

        static bool AreChildrenEqual(IReadOnlyList<HierarchicalLevelBase<TLevel>> thisChildren,
            IReadOnlyList<HierarchicalLevelBase<TLevel>> thatChildren)
        {
            if (thisChildren.Count == 0
                && thatChildren.Count == 0)
            {
                return true;
            }

            if (thisChildren.Count != thatChildren.Count)
            {
                return false;
            }

            foreach (var thisChild in thisChildren)
            {
                foreach (var otherChild in thatChildren)
                {
                    if (!thisChild.Equals(otherChild))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}