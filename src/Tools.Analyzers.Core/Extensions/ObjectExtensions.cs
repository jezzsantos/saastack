using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Common.Extensions;

public static class ObjectExtensions
{
    /// <summary>
    ///     Whether the object does not exist
    /// </summary>
    [ContractAnnotation("null => true; notnull => false")]
    public static bool NotExists(this object? instance)
    {
        return instance is null;
    }
}