using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace QueryAny;

/// <summary>
///     HACK: This is a workaround to include types from the QueryAny library, since it cannot be used in netstandard20
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
[UsedImplicitly]
public class EntityNameAttribute : Attribute
{
    public EntityNameAttribute(string name)
    {
    }
}

public interface IQueryableEntity;