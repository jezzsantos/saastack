using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace Common.Extensions;

[UsedImplicitly]
public static class StringExtensions
{
    public enum JsonCasing
    {
        Pascal,
        Camel
    }

    private static readonly TimeSpan DefaultRegexTimeout = TimeSpan.FromSeconds(10);

    /// <summary>
    ///     Whether the <see cref="other" /> is the same as the value (case-insensitive)
    /// </summary>
    public static bool EqualsIgnoreCase(this string value, string other)
    {
        return string.Equals(value, other, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Whether the <see cref="other" /> is precisely the same as the value (case-sensitive)
    /// </summary>
    public static bool EqualsOrdinal(this string value, string other)
    {
        return string.Equals(value, other, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Formats the <see cref="value" /> with the <see cref="arguments" />
    /// </summary>
    public static string Format(this string value, params object[] arguments)
    {
        return string.Format(value, arguments);
    }

    /// <summary>
    ///     Converts the <see cref="json" /> to an object of the specified <see cref="TResult" />
    /// </summary>
    public static TResult? FromJson<TResult>(this string json)
    {
        if (json.HasNoValue())
        {
            return default;
        }

        return JsonSerializer.Deserialize<TResult>(json, new JsonSerializerOptions());
    }

    /// <summary>
    ///     Whether the string value contains no value: it is either: null, empty or only whitespaces
    /// </summary>
    [ContractAnnotation("null => true; notnull => false")]
    public static bool HasNoValue(this string? value)
    {
        return string.IsNullOrEmpty(value)
               || string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    ///     Whether the string value contains any value except: null, empty or only whitespaces
    /// </summary>
    [ContractAnnotation("null => false; notnull => true")]
    public static bool HasValue(this string? value)
    {
        return !string.IsNullOrEmpty(value)
               && !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    ///     Whether the <see cref="value" /> matches the <see cref="pattern" />
    ///     Avoid potential DOS attacks where the regex may timeout if too complex
    /// </summary>
    public static bool IsMatchWith(this string value, [RegexPattern] string pattern, TimeSpan? timeout = null)
    {
        if (value.NotExists() || pattern.NotExists())
        {
            return false;
        }

        var timeoutSafe = timeout ?? DefaultRegexTimeout;
        return Regex.IsMatch(value, pattern, RegexOptions.None, timeoutSafe);
    }

    /// <summary>
    ///     Whether the <see cref="other" /> is not the same as the value (case-insensitive)
    /// </summary>
    public static bool NotEqualsIgnoreCase(this string value, string other)
    {
        return !value.EqualsIgnoreCase(other);
    }

    /// <summary>
    ///     Whether the <see cref="other" /> is not the same as the value (case-sensitive)
    /// </summary>
    public static bool NotEqualsOrdinal(this string value, string other)
    {
        return !value.EqualsOrdinal(other);
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to a boolean value
    /// </summary>
    public static bool ToBool(this string value)
    {
        if (value.HasNoValue())
        {
            return false;
        }

        return bool.Parse(value);
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to a boolean value,
    ///     and in the case where the value cannot be converted, uses the <see cref="defaultValue" />;
    /// </summary>
    public static bool ToBoolOrDefault(this string value, bool defaultValue)
    {
        if (value.HasNoValue())
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out var converted))
        {
            return converted;
        }

        return defaultValue;
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to a integer value
    /// </summary>
    public static int ToInt(this string value)
    {
        if (value.HasNoValue())
        {
            return -1;
        }

        return int.Parse(value);
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to a integer value,
    ///     and in the case where the value cannot be converted, uses the <see cref="defaultValue" />;
    /// </summary>
    public static int ToIntOrDefault(this string value, int defaultValue)
    {
        if (value.HasNoValue())
        {
            return defaultValue;
        }

        if (int.TryParse(value, out var converted))
        {
            return converted;
        }

        return defaultValue;
    }


    /// <summary>
    ///     Converts the object to a json format
    /// </summary>
    public static string? ToJson(this object? value, bool prettyPrint = true, JsonCasing? casing = null,
        bool includeNulls = false)
    {
        if (value is null)
        {
            return null;
        }

        JsonNamingPolicy namingPolicy = null!; // PascalCase
        if (casing == JsonCasing.Camel)
        {
            namingPolicy = JsonNamingPolicy.CamelCase;
        }

        return JsonSerializer.Serialize(value, new JsonSerializerOptions
        {
            WriteIndented = prettyPrint,
            PropertyNamingPolicy = namingPolicy,
            DefaultIgnoreCondition = includeNulls ? JsonIgnoreCondition.Never : JsonIgnoreCondition.WhenWritingNull
        });
    }
}