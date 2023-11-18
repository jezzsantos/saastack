using Common;
using Common.Extensions;
using Domain.Interfaces;

namespace Domain.Common.Extensions;

public static class HydrationPropertiesExtensions
{
    /// <summary>
    ///     Returns the named value from the <see cref="properties" /> of the matching <see cref="TValue" />,
    ///     or if not exists, returns the <see cref="defaultValue" />
    /// </summary>
    public static Optional<TValue> GetValueOrDefault<TValue>(this HydrationProperties properties,
        string propertyName)

    {
        if (!properties.ContainsKey(propertyName))
        {
            return Optional<TValue>.None;
        }

        var propertyValue = properties[propertyName];
        if (propertyValue.HasValue)
        {
            return new Optional<TValue>((TValue)propertyValue.Value);
        }

        return Optional<TValue>.None;
    }

    /// <summary>
    ///     Returns the named value from the <see cref="properties" /> of the matching <see cref="TValue" />,
    ///     or if not exists, returns the <see cref="defaultValue" />
    /// </summary>
    public static Optional<TValue> GetValueOrDefault<TValue>(this HydrationProperties properties,
        string propertyName, TValue defaultValue)

    {
        if (!properties.ContainsKey(propertyName))
        {
            return defaultValue.Exists()
                ? defaultValue
                : default!;
        }

        var propertyValue = properties[propertyName];
        if (propertyValue.HasValue)
        {
            return new Optional<TValue>((TValue)propertyValue.Value);
        }

        return defaultValue.Exists()
            ? defaultValue
            : Optional<TValue>.None;
    }
}