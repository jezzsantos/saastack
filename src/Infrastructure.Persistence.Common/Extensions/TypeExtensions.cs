using Common;
using Common.Extensions;
using Domain.Interfaces.ValueObjects;

namespace Infrastructure.Persistence.Common.Extensions;

public static class TypeExtensions
{
    private static readonly List<Type> NonComplexTypes =
    [
        typeof(string), typeof(Optional<string>), typeof(Optional<string?>), typeof(DateTime), typeof(DateTime?),
        typeof(Optional<DateTime>), typeof(Optional<DateTime?>), typeof(DateTimeOffset), typeof(DateTimeOffset?),
        typeof(Optional<DateTimeOffset>), typeof(Optional<DateTimeOffset?>), typeof(bool), typeof(bool?),
        typeof(Optional<bool>), typeof(Optional<bool?>), typeof(int), typeof(int?), typeof(Optional<int>),
        typeof(Optional<int?>), typeof(long), typeof(long?), typeof(Optional<long>), typeof(Optional<long?>),
        typeof(double), typeof(double?), typeof(Optional<double>), typeof(Optional<double?>), typeof(decimal),
        typeof(decimal?), typeof(Optional<decimal>), typeof(Optional<decimal?>), typeof(byte[]),
        typeof(Guid), typeof(Guid?), typeof(Optional<Guid>), typeof(Optional<Guid?>)
    ];

    /// <summary>
    ///     Whether the <see cref="type" /> is considered a complex type
    /// </summary>
    public static bool IsComplexStorageType(this Type type)
    {
        if (NonComplexTypes.Contains(type))
        {
            return false;
        }

        if (type.IsEnum || type.IsNullableEnum() || type.IsOptionalEnum())
        {
            return false;
        }

        if (typeof(IDehydratableValueObject).IsAssignableFrom(type))
        {
            return false;
        }

        if (Optional.IsOptionalType(type, out var containedType))
        {
            if (typeof(IDehydratableValueObject).IsAssignableFrom(containedType))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Whether the <see cref="type" /> represents a <see cref="Nullable{Enum}" />
    /// </summary>
    public static bool IsNullableEnum(this Type type)
    {
        return Nullable.GetUnderlyingType(type)?.IsEnum == true;
    }

    /// <summary>
    ///     Whether the <see cref="type" /> represents a <see cref="Optional{Enum}" />
    /// </summary>
    public static bool IsOptionalEnum(this Type type)
    {
        if (!Optional.IsOptionalType(type))
        {
            return false;
        }

        if (Optional.TryGetContainedType(type, out var containedType))
        {
            return containedType!.IsEnum;
        }

        return false;
    }

    /// <summary>
    ///     Returns the value of the <see cref="Nullable{Enum}" /> if the <see cref="type" /> is an Enum.
    /// </summary>
    public static object ParseNullable(this Type type, string value)
    {
        var enumType = Nullable.GetUnderlyingType(type);
        if (enumType.NotExists())
        {
            throw new InvalidOperationException(Resources.TypeExtensions_InvalidType.Format(type.ToString()));
        }

        return Enum.Parse(enumType, value, true);
    }
}