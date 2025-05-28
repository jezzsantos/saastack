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
        if (properties.TryGetValue(propertyName, out var propertyValue))
        {
            if (propertyValue.HasValue)
            {
                var value = propertyValue.Value;
                return value.IsOptional(out var optional)
                    ? new Optional<TValue>((TValue?)optional)
                    : new Optional<TValue>((TValue)value);
            }

            return Optional<TValue>.None;
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
        if (properties.TryGetValue(propertyName, out var propertyValue))
        {
            if (propertyValue.HasValue)
            {
                var value = propertyValue.Value;
                return value.IsOptional(out var optional)
                    ? new Optional<TValue>((TValue?)optional)
                    : new Optional<TValue>((TValue)value);
            }

            return defaultValue.Exists()
                ? defaultValue
                : Optional<TValue>.None;
        }

        return defaultValue.Exists()
            ? defaultValue
            : default!;
    }
}