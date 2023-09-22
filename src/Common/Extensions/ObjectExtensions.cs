using JetBrains.Annotations;

namespace Common.Extensions;

public static class ObjectExtensions
{
    /// <summary>
    ///     Whether the object does exist
    /// </summary>
    [ContractAnnotation("null => false; notnull => true")]
    public static bool Exists(this object? instance)
    {
        return instance is not null;
    }

    /// <summary>
    ///     Whether the object does not exist
    /// </summary>
    [ContractAnnotation("null => true; notnull => false")]
    public static bool NotExists(this object? instance)
    {
        return instance is null;
    }
}