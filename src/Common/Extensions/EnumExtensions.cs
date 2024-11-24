using System.Reflection;
using System.Runtime.Serialization;

namespace Common.Extensions;

public static class EnumExtensions
{
    /// <summary>
    ///     Converts the <see cref="value" /> to an value of the <see cref="TTargetEnum" />
    /// </summary>
    public static TTargetEnum ToEnum<TTargetEnum>(this string value)
    {
        return (TTargetEnum)Enum.Parse(typeof(TTargetEnum), value, true);
    }

    /// <summary>
    ///     Converts the value of the <see cref="TSourceEnum" /> to a value in the <see cref="TTargetEnum" />
    /// </summary>
    public static TTargetEnum ToEnum<TSourceEnum, TTargetEnum>(this TSourceEnum source)
        where TSourceEnum : Enum
        where TTargetEnum : Enum
    {
        return source.ToString()
            .ToEnum<TTargetEnum>();
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to an value of the <see cref="TTargetEnum" />,
    ///     and in the case where no value can be found, uses the <see cref="defaultValue" />
    /// </summary>
    public static TTargetEnum ToEnumOrDefault<TTargetEnum>(this string? value, TTargetEnum defaultValue)
    {
        if (value.HasNoValue())
        {
            return defaultValue;
        }

        if (Enum.TryParse(typeof(TTargetEnum), value, true, out var converted))
        {
            return (TTargetEnum)converted;
        }

        return defaultValue;
    }

    /// <summary>
    ///     Converts the value of the <see cref="TSourceEnum" /> to a value in the <see cref="TTargetEnum" />,
    ///     and in the case where no value can be found, uses the <see cref="defaultValue" />
    /// </summary>
    public static TTargetEnum ToEnumOrDefault<TSourceEnum, TTargetEnum>(this TSourceEnum source,
        TTargetEnum defaultValue)
        where TSourceEnum : Enum
        where TTargetEnum : Enum
    {
        var sourceValue = source.ToString();
        if (Enum.TryParse(typeof(TTargetEnum), sourceValue, true, out var converted))
        {
            return (TTargetEnum)converted;
        }

        return defaultValue;
    }

    /// <summary>
    ///     Converts the value to its string representation, or
    ///     to the value of the <see cref="EnumMemberAttribute" /> if it exists
    /// </summary>
    public static string ToString<TEnum>(this TEnum value, bool useEnumMember = false)
        where TEnum : Enum
    {
        if (useEnumMember)
        {
            var enumType = typeof(TEnum);
            var member = enumType.GetMember(value.ToString());
            if (member.Length > 0)
            {
                var attribute = member[0].GetCustomAttribute<EnumMemberAttribute>(false);
                if (attribute.Exists())
                {
                    return attribute.Value ?? value.ToString();
                }
            }
        }

        return value.ToString();
    }
}