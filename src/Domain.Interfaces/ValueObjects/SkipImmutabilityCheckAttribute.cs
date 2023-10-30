namespace Domain.Interfaces.ValueObjects;

/// <summary>
///     Marks the method as being ignored by any immutability tests
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class SkipImmutabilityCheckAttribute : Attribute
{
}