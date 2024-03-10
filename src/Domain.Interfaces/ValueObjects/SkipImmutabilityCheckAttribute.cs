namespace Domain.Interfaces.ValueObjects;

/// <summary>
///     Marks the method as being ignored by any immutability checks (performed by the Roslyn analyzers)
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class SkipImmutabilityCheckAttribute : Attribute;