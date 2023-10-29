using System.Text.Json;
using System.Text.Json.Serialization;
using JetBrains.Annotations;

// ReSharper disable once CheckNamespace
namespace Common.Extensions;

public static class StringExtensions
{
    public enum JsonCasing
    {
        Pascal,
        Camel
    }

    /// <summary>
    ///     Formats the <see cref="value" /> with the <see cref="arguments" />
    /// </summary>
    public static string Format(this string value, params object[] arguments)
    {
        return string.Format(value, arguments);
    }

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
            DefaultIgnoreCondition = includeNulls
                ? JsonIgnoreCondition.Never
                : JsonIgnoreCondition.WhenWritingNull
        });
    }
}