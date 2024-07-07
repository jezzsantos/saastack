namespace Domain.Interfaces.ValueObjects;

/// <summary>
///     Skips immutability checks performed on all methods of valueobjects to ensure immutability.
///     Used for methods that have other utility, that definitely do not mutate the state of this instance
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class SkipImmutabilityCheckAttribute : Attribute;