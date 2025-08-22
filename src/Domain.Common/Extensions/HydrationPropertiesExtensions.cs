using Common;
using Common.Extensions;
using Domain.Interfaces;

namespace Domain.Common.Extensions;

public static class HydrationPropertiesExtensions
{
    /// <summary>
    ///     Returns the named value from the <see cref="properties" /> of the matching <see cref="TValue" />,
    ///     or if not exists, returns the <see cref="Optional{TValue}.None" />
    /// </summary>
    public static Optional<TValue> GetValueOrDefault<TValue>(this HydrationProperties properties, string propertyName)
    {
        if (properties.TryGetValue(propertyName, out var propertyValue))
        {
            if (propertyValue.HasValue)
            {
                var value = propertyValue.Value;

                if (value.IsOptional(out var descriptor))
                {
                    var containedValue = descriptor!.ContainedValue;
                    return new Optional<TValue>((TValue?)containedValue);
                }

                return new Optional<TValue>((TValue)value);
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
                if (value.IsOptional(out var descriptor))
                {
                    var containedValue = descriptor!.ContainedValue;
                    return new Optional<TValue>((TValue?)containedValue);
                }

                return new Optional<TValue>((TValue)value);
            }

            return defaultValue.Exists()
                ? new Optional<TValue>(defaultValue)
                : Optional<TValue>.None;
        }

        return defaultValue.Exists()
            ? new Optional<TValue>(defaultValue)
            : new Optional<TValue>(default(TValue));
    }
}