using Common.Extensions;
using Domain.Interfaces.ValueObjects;

namespace Infrastructure.Persistence.Common;

public static class TypeExtensions
{
    /// <summary>
    ///     Whether the <see cref="type" /> is considered a complex type
    /// </summary>
    public static bool IsComplexStorageType(this Type type)
    {
        if (type == typeof(string)
            || type.IsEnum || type.IsNullableEnum()
            || type == typeof(DateTime) || type == typeof(DateTime?)
            || type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?)
            || type == typeof(bool) || type == typeof(bool?)
            || type == typeof(int) || type == typeof(int?)
            || type == typeof(long) || type == typeof(long?)
            || type == typeof(double) || type == typeof(double?)
            || type == typeof(byte[])
            || type == typeof(Guid) || type == typeof(Guid?)
            || typeof(IDehydratableValueObject).IsAssignableFrom(type)
           )
        {
            return false;
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