using Domain.Interfaces.ValueObjects;

namespace Domain.Common.ValueObjects;

public static class IdentifierExtensions
{
    /// <summary>
    ///     Converts a <see cref="string" /> to an <see cref="Identifier" />
    /// </summary>
    public static Identifier ToIdentifier(this string id)
    {
        return Identifier.Create(id);
    }

    /// <summary>
    ///     Converts a <see cref="string" /> to an <see cref="Identifier" />
    /// </summary>
    public static Identifier ToId(this string id)
    {
        return id.ToIdentifier();
    }
}