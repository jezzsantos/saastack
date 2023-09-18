using System.Text.Json;
using System.Text.Json.Serialization;

namespace Common.Extensions;

public static class StringExtensions
{
    public enum JsonCasing
    {
        Pascal,
        Camel
    }

    /// <summary>
    ///     Whether the string value contains any value except: null, empty or only whitespaces
    /// </summary>
    public static bool HasValue(this string? value)
    {
        return !string.IsNullOrEmpty(value)
               && !string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    ///     Whether the string value contains no value: it is either: null, empty or only whitespaces
    /// </summary>
    public static bool HasNoValue(this string? value)
    {
        return string.IsNullOrEmpty(value)
               || string.IsNullOrWhiteSpace(value);
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