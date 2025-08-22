using Common;
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

    /// <summary>
    ///     We dehydrate individual values using JSON serialization on each value, which ensures that the
    ///     underlying type is serialized appropriately.
    ///     For example:
    ///     A <see cref="DateTime" /> value will be serialized as an ISO7801 string,
    ///     An <see cref="Enum" /> type will be serialized to a string value,
    ///     An <see cref="Optional{TValue}" /> type will be serialized to a string value
    ///     Any missing value is serialized as <see cref="NullValue" />
    /// </summary>
    private static string Dehydrate(object? value)
    {
        if (value.NotExists())
        {
            return NullValue;
        }

        var dehydrated = value.ToJson(false, StringExtensions.JsonCasing.Camel);
        return dehydrated.Exists()
            ? dehydrated
            : NullValue;
    }

    private static TResult Rehydrate<TResult>(string? dehydrated)
        where TResult : new()
    {
        if (dehydrated.NotExists())
        {
            return new TResult();
        }

        var rehydrated = dehydrated.FromJson<TResult>();
        return rehydrated.Exists()
            ? rehydrated
            : new TResult();
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

            return Dehydrate(value);
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

    protected static List<Optional<string>> RehydrateToList(string hydratedValue, bool isSingleValueObject,
        bool isSingleListValueObject = false)
    {
        if (isSingleValueObject)
        {
            if (isSingleListValueObject)
            {
                return Rehydrate<List<string>>(hydratedValue)
                    .Select(value => value.Equals(NullValue)
                        ? Optional<string>.None
                        : new Optional<string>(value))
                    .ToList();
            }

            if (hydratedValue.NotExists())
            {
                return [];
            }

            return hydratedValue.Equals(NullValue)
                ? [Optional<string>.None]
                : [hydratedValue];
        }

        return Rehydrate<Dictionary<string, object>>(hydratedValue)
            .Select(pair =>
            {
                if (pair.Value.NotExists())
                {
                    return Optional<string>.None;
                }

                var value = pair.Value.ToString();
                if (value.NotExists())
                {
                    return Optional<string>.None;
                }

                return value.Equals(NullValue)
                    ? Optional<string>.None
                    : new Optional<string>(value);
            })
            .ToList();
    }

    private static object DehydrateInternal(object? value)
    {
        if (value is null)
        {
            return NullValue;
        }

        if (value.IsOptional(out var descriptor))
        {
            if (descriptor.NotExists())
            {
                return NullValue;
            }

            if (descriptor.IsNone)
            {
                return NullValue;
            }

            //Unpack any Optional wrappers
            return DehydrateInternal(descriptor.ContainedValue);
        }
        

        if (value is IDehydratableValueObject valueObject)
        {
            return valueObject.Dehydrate();
        }

        if (value is IEnumerable<IDehydratableValueObject> enumerable)
        {
            return enumerable
                .Select(item => item.Exists()
                    ? item.Dehydrate()
                    : default)
                .ToList();
        }

        return value;
    }
}