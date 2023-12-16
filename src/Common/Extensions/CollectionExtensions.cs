using System.Text;
using JetBrains.Annotations;

namespace Common.Extensions;

[UsedImplicitly]
public static class CollectionExtensions
{
    /// <summary>
    ///     Whether the <see cref="target" /> string exists in the <see cref="collection" />
    /// </summary>
    public static bool ContainsIgnoreCase(this IEnumerable<string> collection, string target)
    {
        if (target.HasNoValue())
        {
            return false;
        }

        return collection.Any(item => item.EqualsIgnoreCase(target));
    }

    /// <summary>
    ///     Returns the first item in the collection
    /// </summary>
    public static TResult First<TResult>(this IReadOnlyList<TResult> list)
    {
        return list[0];
    }

    /// <summary>
    ///     Whether the collection contains any items
    /// </summary>
    public static bool HasAny<T>(this IEnumerable<T>? collection)
    {
        if (collection.NotExists())
        {
            return false;
        }

        return !collection.HasNone();
    }

    /// <summary>
    ///     Whether the collection contains no items
    /// </summary>
    public static bool HasNone<T>(this IEnumerable<T> collection)
    {
        return !collection.Any();
    }

    /// <summary>
    ///     Joins all values separated by the <see cref="separator" />
    /// </summary>
    public static string Join<T>(this IEnumerable<T> values, string separator)
    {
        var stringBuilder = new StringBuilder();
        foreach (var value in values)
        {
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append(separator);
            }

            stringBuilder.Append(value);
        }

        return stringBuilder.ToString();
    }

    /// <summary>
    ///     Returns the last item in the collection
    /// </summary>
    public static TResult Last<TResult>(this IReadOnlyList<TResult> list)
    {
        return list[^1];
    }

    /// <summary>
    ///     Whether the <see cref="target" /> string does not exist in the <see cref="collection" />
    /// </summary>
    public static bool NotContainsIgnoreCase(this IEnumerable<string> collection, string target)
    {
        return !collection.ContainsIgnoreCase(target);
    }
}