namespace Infrastructure.WebApi.Generators.Extensions;

public static class StringExtensions
{
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
}