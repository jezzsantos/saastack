using JetBrains.Annotations;

namespace Common;

/// <summary>
///     Defines a marker type for value types
/// </summary>
// ReSharper disable once UnusedTypeParameter
[UsedImplicitly]
public sealed class ValueTypeTag<T>
    where T : struct
{
}

/// <summary>
///     Defines a marker type for reference types
/// </summary>
// ReSharper disable once UnusedTypeParameter
[UsedImplicitly]
public sealed class ReferenceTypeTag<T>
    where T : class?
{
}