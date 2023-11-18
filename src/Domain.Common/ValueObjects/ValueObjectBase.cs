using Common.Extensions;
using Domain.Interfaces.ValueObjects;

namespace Domain.Common.ValueObjects;

/// <summary>
///     Defines a DDD value object.
///     Value objects are immutable, and their properties should be set at construction, and never altered.
///     Value objects are equal when their internal data is the same.
///     Value objects support being persisted
/// </summary>
public abstract partial class ValueObjectBase<TValueObject> : IValueObject
{
    internal const string NullValue = "NULL";

    private static string Dehydrate(object? value)
    {
        if (value.NotExists())
        {
            return NullValue;
        }

        var dehydrated = value.ToJson(false, StringExtensions.JsonCasing.Camel);
        if (dehydrated.Exists())
        {
            return dehydrated;
        }

        return NullValue;
    }

    private static TResult Rehydrate<TResult>(string? dehydrated)
        where TResult : new()
    {
        if (dehydrated.NotExists())
        {
            return new TResult();
        }

        var rehydrated = dehydrated.FromJson<TResult>();
        if (rehydrated.Exists())
        {
            return rehydrated;
        }

        return new TResult();
    }

    protected abstract IEnumerable<object?> GetAtomicValues();

    [SkipImmutabilityCheck]
    public virtual string Dehydrate()
    {
        var parts = GetAtomicValues().ToList();
        if (parts.HasNone())
        {
            return NullValue;
        }

        if (parts.Count == 1)
        {
            var value = DehydrateInternal(parts[0]);
            if (value.NotExists())
            {
                return NullValue;
            }

            if (value is string)
            {
                return value.ToString() ?? NullValue;
            }

            if (value is Enum)
            {
                return value.ToString() ?? NullValue;
            }

            return Dehydrate(DehydrateInternal(parts[0]));
        }

        var counter = 1;
        var properties = parts
            .ToDictionary(_ => $"Val{counter++}", DehydrateInternal);
        return Dehydrate(properties);
    }

    [SkipImmutabilityCheck]
    public override string ToString()
    {
        return Dehydrate();
    }

    protected static List<string> RehydrateToList(string hydratedValue, bool isSingleValueObject,
        bool isSingleListValueObject = false)
    {
        if (isSingleValueObject)
        {
            if (isSingleListValueObject)
            {
                return Rehydrate<List<string>>(hydratedValue)
                    .Where(value => value.HasValue() && !value.Equals(NullValue))
                    .Select(value => value)
                    .ToList();
            }

            if (hydratedValue.NotExists())
            {
                return new List<string>();
            }

            return hydratedValue.Equals(NullValue)
                ? new List<string>()
                : new List<string> { hydratedValue };
        }

        return Rehydrate<Dictionary<string, object>>(hydratedValue)
            .Where(pair => !pair.Value.Equals(NullValue))
            .Select(pair =>
            {
                var value = pair.Value.ToString()!;
                return value.Equals(NullValue)
                    ? null!
                    : value;
            })
            .ToList();
    }

    private static object DehydrateInternal(object? value)
    {
        if (value is null)
        {
            return NullValue;
        }

        if (value is IDehydratableValueObject valueObject)
        {
            return valueObject.Dehydrate();
        }

        if (value is IEnumerable<IDehydratableValueObject> enumerable)
        {
            return enumerable
                .Select(e => e.Exists()
                    ? e.Dehydrate()
                    : default)
                .ToList();
        }

        return value;
    }
}