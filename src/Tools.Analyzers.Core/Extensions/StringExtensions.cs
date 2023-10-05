using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Common.Extensions;

public static class StringExtensions
{
    /// <summary>
    ///     Whether the string value contains no value: it is either: null, empty or only whitespaces
    /// </summary>
    [ContractAnnotation("null => true; notnull => false")]
    public static bool HasNoValue(this string? value)
    {
        return string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    ///     Whether the string value contains any value except: null, empty or only whitespaces
    /// </summary>
    [ContractAnnotation("null => false; notnull => true")]
    public static bool HasValue(this string? value)
    {
        return !string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    ///     Formats the <see cref="value" /> with the <see cref="arguments" />
    /// </summary>
    public static string Format(this string value, params object[] arguments)
    {
        return string.Format(value, arguments);
    }
}