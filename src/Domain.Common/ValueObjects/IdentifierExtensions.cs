using System.Diagnostics;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Interfaces.ValueObjects;

namespace Domain.Common.ValueObjects;

public static class IdentifierExtensions
{
    /// <summary>
    ///     Whether the <see cref="valueObject" /> has an empty value
    /// </summary>
    public static bool IsEmpty(this ISingleValueObject<string> valueObject)
    {
        return !valueObject.Value.HasValue();
    }

    /// <summary>
    ///     Converts a <see cref="string" /> to an <see cref="Identifier" />
    /// </summary>
    [DebuggerStepThrough]
    public static Identifier ToId(this string id)
    {
        return Identifier.Create(id);
    }

    /// <summary>
    ///     Returns a, <see cref="IIdentifierFactory" /> that creates identifiers with the <see cref="identifier" /> specified
    /// </summary>
    [DebuggerStepThrough]
    public static IIdentifierFactory ToIdentifierFactory(this string identifier)
    {
        return new FixedIdentifierFactory(identifier);
    }

    /// <summary>
    ///     Returns a, <see cref="IIdentifierFactory" /> that creates identifiers with the <see cref="Identifier" /> specified
    /// </summary>
    [DebuggerStepThrough]
    public static IIdentifierFactory ToIdentifierFactory(this Identifier identifier)
    {
        return new FixedIdentifierFactory(identifier);
    }
}