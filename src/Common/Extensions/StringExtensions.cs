#if COMMON_PROJECT
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
#endif
#if COMMON_PROJECT || GENERATORS_WEB_API_PROJECT || GENERATORS_COMMON_PROJECT || ANALYZERS_NONPLATFORM
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
#endif

#if GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
#endif

#if GENERATORS_COMMON_PROJECT
using System.Globalization;
using System.Text;
#endif

namespace Common.Extensions;

#if COMMON_PROJECT
[UsedImplicitly]
#endif
public static class StringExtensions
{
#if COMMON_PROJECT || GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM
    /// <summary>
    ///     Defines the casing used in JSON serialization
    /// </summary>
    public enum JsonCasing
    {
        Pascal,
        Camel
    }
#endif
#if COMMON_PROJECT
    private static readonly TimeSpan DefaultRegexTimeout = TimeSpan.FromSeconds(10);
#endif
#if COMMON_PROJECT || GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM
    /// <summary>
    ///     Whether the <see cref="other" /> is the same as the value (case-insensitive)
    /// </summary>
    public static bool EqualsIgnoreCase(this string? value, string? other)
    {
        return string.Equals(value, other, StringComparison.OrdinalIgnoreCase);
    }
#endif
#if COMMON_PROJECT || ANALYZERS_NONPLATFORM
    /// <summary>
    ///     Whether the <see cref="other" /> is precisely the same as the value (case-sensitive)
    /// </summary>
    public static bool EqualsOrdinal(this string? value, string? other)
    {
        return string.Equals(value, other, StringComparison.Ordinal);
    }
#endif
#if COMMON_PROJECT || ANALYZERS_NONPLATFORM || ANALYZERS_NONPLATFORM
    /// <summary>
    ///     Formats the <see cref="value" /> with the <see cref="arguments" />
    /// </summary>
    public static string Format(this string value, params object[] arguments)
    {
        return string.Format(value, arguments);
    }
#endif
#if COMMON_PROJECT
    /// <summary>
    ///     Converts the <see cref="json" /> to an object of the specified <see cref="TResult" />
    /// </summary>
    public static TResult? FromJson<TResult>(this string json)
    {
        if (json.HasNoValue())
        {
            return default;
        }

        return JsonSerializer.Deserialize<TResult>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters =
            {
                new OptionalConverterFactory()
            }
        });
    }
#elif GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM
    public static TObject FromJson<TObject>(this string json)
        where TObject : new()
    {
        if (json.HasNoValue())
        {
            return new TObject();
        }

        var deserialized = FromJson(json, typeof(TObject));
        if (deserialized is not null)
        {
            return (TObject)deserialized;
        }

        return new TObject();
    }

    public static object? FromJson(this string json, Type type)
    {
        if (json.HasNoValue())
        {
            return null;
        }

        var serializer = new DataContractJsonSerializer(type, new DataContractJsonSerializerSettings());

        using (var ms = new MemoryStream(Encoding.Unicode.GetBytes(json)))
        {
            return serializer.ReadObject(ms);
        }
    }
#endif
#if COMMON_PROJECT
    /// <summary>
    ///     Converts the <see cref="json" /> to an object of the specified <see cref="type" />
    /// </summary>
    public static object? FromJson(this string json, Type type)
    {
        if (json.HasNoValue())
        {
            return default;
        }

        return JsonSerializer.Deserialize(json, type, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
    }
#endif
#if COMMON_PROJECT || GENERATORS_WEB_API_PROJECT || GENERATORS_COMMON_PROJECT || ANALYZERS_NONPLATFORM
    /// <summary>
    ///     Whether the string value contains no value: it is either: null, empty or only whitespaces
    /// </summary>
    [ContractAnnotation("null => true; notnull => false")]
    [DebuggerStepThrough]
    public static bool HasNoValue([NotNullWhen(false)] this string? value)
    {
        return string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    ///     Whether the string value contains any value except: null, empty or only whitespaces
    /// </summary>
    [ContractAnnotation("null => false; notnull => true")]
    [DebuggerStepThrough]
    public static bool HasValue([NotNullWhen(true)] this string? value)
    {
        return !string.IsNullOrEmpty(value) && !string.IsNullOrWhiteSpace(value);
    }
#endif
#if COMMON_PROJECT
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
    ///     This method ensures that we apply a timeout to the matching,
    ///     to avoid potential DOS attacks where a regular expression is used on on outer boundary layer
    /// </summary>
    public static string ReplaceWith(this string value, [RegexPattern] string pattern, string replacement,
        TimeSpan? timeout = null)
    {
        if (value.HasNoValue() || pattern.HasNoValue())
        {
            return value;
        }

        var timeoutSafe = timeout ?? DefaultRegexTimeout;
        return Regex.Replace(value, pattern, replacement, RegexOptions.None, timeoutSafe);
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to a boolean value
    /// </summary>
    public static bool ToBool(this string? value)
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
#endif
#if COMMON_PROJECT
    /// <summary>
    ///     Returns the specified <see cref="stringValue" /> in camelCase. i.e. first letter is lower case
    /// </summary>
    public static string ToCamelCase(this string value)
    {
        if (value.HasNoValue())
        {
            return value;
        }

        return JsonNamingPolicy.CamelCase
            .ConvertName(value)
            .Replace(" ", string.Empty);
    }
#elif GENERATORS_COMMON_PROJECT
    /// <summary>
    ///     Returns the specified <see cref="stringValue" /> in camelCase. i.e. first letter is lower case
    /// </summary>
    public static string ToCamelCase(this string value)
    {
        if (value.HasNoValue())
        {
            return value;
        }

        var titleCase = value.ToTitleCase()
            .Replace(" ", string.Empty);

        return char.ToLowerInvariant(titleCase[0]) + titleCase.Substring(1);
    }
#endif
#if COMMON_PROJECT || ANALYZERS_NONPLATFORM
    /// <summary>
    ///     Converts the <see cref="value" /> to a floating value
    /// </summary>
    public static double ToDouble(this string? value)
    {
        if (value.HasNoValue())
        {
            return -1;
        }

        return double.Parse(value);
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to an integer value
    /// </summary>
    public static int ToInt(this string? value)
    {
        if (value.HasNoValue())
        {
            return -1;
        }

        return int.Parse(value);
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to an integer value,
    ///     and in the case where the value cannot be converted, uses the <see cref="defaultValue" />
    /// </summary>
    public static int ToIntOrDefault(this string? value, int defaultValue)
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
    ///     Converts the <see cref="value" /> to a decimal value
    /// </summary>
    public static decimal ToDecimal(this string? value)
    {
        if (value.HasNoValue())
        {
            return -1;
        }

        return decimal.Parse(value);
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to a decimal value,
    ///     and in the case where the value cannot be converted, uses the <see cref="defaultValue" />
    /// </summary>
    public static decimal ToDecimalOrDefault(this string? value, decimal defaultValue)
    {
        if (value.HasNoValue())
        {
            return defaultValue;
        }

        if (decimal.TryParse(value, out var converted))
        {
            return converted;
        }

        return defaultValue;
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to a long value
    /// </summary>
    public static long ToLong(this string? value)
    {
        if (value.HasNoValue())
        {
            return -1;
        }

        return long.Parse(value);
    }

    /// <summary>
    ///     Converts the <see cref="value" /> to a long value,
    ///     and in the case where the value cannot be converted, uses the <see cref="defaultValue" />
    /// </summary>
    public static long ToLongOrDefault(this string? value, long defaultValue)
    {
        if (value.HasNoValue())
        {
            return defaultValue;
        }

        if (long.TryParse(value, out var converted))
        {
            return converted;
        }

        return defaultValue;
    }
#endif
#if COMMON_PROJECT
    /// <summary>
    ///     Converts the object to a json format
    /// </summary>
    public static string? ToJson(this object? value, bool prettyPrint = true, JsonCasing casing = JsonCasing.Pascal,
        bool includeNulls = false)
    {
        if (value is null)
        {
            return null;
        }

        JsonNamingPolicy namingPolicy = null!; // null implies PascalCase
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
                : JsonIgnoreCondition.WhenWritingNull,
            Converters =
            {
                new OptionalConverterFactory()
            }
        });
    }
#elif GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM || ANALYZERS_NONPLATFORM
    public static string? ToJson<TObject>(this TObject? value, bool? prettyPrint = true, JsonCasing casing =
        JsonCasing.Pascal)
    {
        if (value is null)
        {
            return null;
        }

        using var stream = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(TObject), new DataContractJsonSerializerSettings
        {
            UseSimpleDictionaryFormat = !(prettyPrint ?? false),
            EmitTypeInformation = EmitTypeInformation.Never
        });
        serializer.WriteObject(stream, value);
        stream.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(stream);

        var result = reader.ReadToEnd();

        return result;
    }
#endif
#if COMMON_PROJECT || GENERATORS_COMMON_PROJECT
    /// <summary>
    ///     Returns the specified <see cref="value" /> in snake_case. i.e. lower case with underscores for upper cased
    ///     letters
    /// </summary>
    public static string ToSnakeCase(this string value)
    {
        if (value.HasNoValue())
        {
            return value;
        }

        value = value
            .Replace(" ", "_")
            .ToCamelCase();

        var builder = new StringBuilder();
        var isFirstCharacter = true;
        var lastCharWasUnderscore = false;
        foreach (var charValue in value)
        {
            if (isFirstCharacter)
            {
                isFirstCharacter = false;
                builder.Append(char.ToLower(charValue));
                continue;
            }

            if (IsIgnoredCharacter(charValue))
            {
                builder.Append(charValue);
                if (charValue == '_')
                {
                    lastCharWasUnderscore = true;
                }
            }
            else
            {
                if (lastCharWasUnderscore)
                {
                    builder.Append(char.ToLower(charValue));
                    lastCharWasUnderscore = false;
                }
                else
                {
                    builder.Append('_');
                    builder.Append(char.ToLower(charValue));
                }
            }
        }

        return builder.ToString();

        static bool IsIgnoredCharacter(char charValue)
        {
            return char.IsDigit(charValue)
                   || (char.IsLetter(charValue) && char.IsLower(charValue))
                   || charValue == '_';
        }
    }
#endif
#if COMMON_PROJECT || GENERATORS_COMMON_PROJECT
    /// <summary>
    ///     Returns the specified <see cref="value" /> in title-case. i.e. first letter of words are capitalized
    /// </summary>
    public static string ToTitleCase(this string value)
    {
        return CultureInfo.InvariantCulture.TextInfo
            .ToTitleCase(value)
            .Replace("_", string.Empty);
    }
#endif
# if COMMON_PROJECT
    /// <summary>
    ///     Returns the specified <see cref="value" /> including only letters (no numbers, or whitespace)
    /// </summary>
    public static string TrimNonAlpha(this string value)
    {
        if (value.HasNoValue())
        {
            return value;
        }

        return value.ReplaceWith(@"[^\p{L}]", string.Empty);
    }

    /// <summary>
    ///     Returns the specified <see cref="path" /> without any leading slashes
    /// </summary>
    public static string WithoutLeadingSlash(this string path)
    {
        return path.TrimStart('/');
    }

    /// <summary>
    ///     Returns the specified <see cref="path" /> without any trailing slashes
    /// </summary>
    public static string WithoutTrailingSlash(this string path)
    {
        return path.TrimEnd('/');
    }

    /// <summary>
    ///     Returns the specified <see cref="path" /> including a trailing slash
    /// </summary>
    public static string WithTrailingSlash(this string path)
    {
        return path.TrimEnd('/') + '/';
    }
#endif
}