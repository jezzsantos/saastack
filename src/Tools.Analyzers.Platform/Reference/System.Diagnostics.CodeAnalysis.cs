#define INTERNAL_NULLABLE_ATTRIBUTES

// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis;

/// <summary>
///     HACK: This code is to enable the use of the '[NotNullWhen]' attribute that is not present in netstandard20
///     Specifies that when a method returns <see cref="ReturnValue" />, the parameter will not be null even if the
///     corresponding type allows it.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter)]
#if INTERNAL_NULLABLE_ATTRIBUTES
internal
#else
    public
#endif
    sealed class NotNullWhenAttribute : Attribute
{
    /// <summary>Initializes the attribute with the specified return value condition.</summary>
    /// <param name="returnValue">
    ///     The return value condition. If the method returns this value, the associated parameter will not be null.
    /// </param>
    public NotNullWhenAttribute(bool returnValue)
    {
        ReturnValue = returnValue;
    }

    /// <summary>Gets the return value condition.</summary>
    public bool ReturnValue { get; }
}