using System.Diagnostics;
using Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using JetBrains.Annotations;

namespace Domain.Common.ValueObjects;

/// <summary>
///     Provides an identifier
/// </summary>
[DebuggerStepThrough]
public sealed class Identifier : SingleValueObjectBase<Identifier, string>
{
    public static Identifier Create(string value)
    {
        return new Identifier(value);
    }

    private Identifier(string value) : base(value)
    {
    }

    public string Text => Value;

    [UsedImplicitly]
    public static ValueObjectFactory<Identifier> Rehydrate()
    {
        return (property, _) => new Identifier(property);
    }

    public static Identifier Empty()
    {
        return new Identifier(string.Empty);
    }

    [SkipImmutabilityCheck]
    public bool IsEmpty()
    {
        return !Value.HasValue();
    }
}