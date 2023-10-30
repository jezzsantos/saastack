namespace Domain.Common.Entities;

public static class EntityExtensions
{
    /// <summary>
    ///     Returns the named value from the <see cref="properties" /> of the matching <see cref="TValue" />,
    ///     or if not exists, returns the <see cref="defaultValue" />
    /// </summary>
    public static TValue? GetValueOrDefault<TValue>(this IReadOnlyDictionary<string, object?> properties,
        string propertyName, TValue? defaultValue = default)
    {
        if (!properties.ContainsKey(propertyName))
        {
            return defaultValue;
        }

        return properties[propertyName] is TValue
            ? (TValue?)properties[propertyName]
            : default;
    }
}