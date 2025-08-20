#if COMMON_PROJECT
using JetBrains.Annotations;
#endif

namespace Common.Extensions;

#if COMMON_PROJECT
[UsedImplicitly]
#endif

public static class CollectionExtensions
{
#if COMMON_PROJECT || GENERATORS_WEB_API_PROJECT || ANALYZERS_NONFRAMEWORK || GENERATORS_WORKERS_PROJECT
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
#endif
#if COMMON_PROJECT
    /// <summary>
    ///     Returns the first item in the collection
    /// </summary>
    public static TResult First<TResult>(this IReadOnlyList<TResult> list)
    {
        return list[0];
    }
#endif
#if COMMON_PROJECT || ANALYZERS_NONFRAMEWORK
    /// <summary>
    ///     Joins all values separated by the <see cref="separator" />
    /// </summary>
    public static string Join<T>(this IEnumerable<T> collection, string separator)
    {
        var stringCollection = collection
            .Select(item => item?.ToString())
            .Where(item => item.HasValue())
            .ToList();

        return string.Join(separator, stringCollection);
    }
#endif
#if COMMON_PROJECT || ANALYZERS_NONFRAMEWORK
    /// <summary>
    ///     Whether the specified collection contains an item that matched the specified <see cref="predicate" />
    /// </summary>
    public static bool NotContains<T>(this IEnumerable<T> collection, Predicate<T> predicate)
    {
        return !collection.Any(item => predicate(item));
    }

    /// <summary>
    ///     Whether the <see cref="target" /> string does not exist in the <see cref="collection" />
    /// </summary>
    public static bool NotContainsIgnoreCase(this IEnumerable<string> collection, string target)
    {
        return !collection.ContainsIgnoreCase(target);
    }
#endif
#if COMMON_PROJECT || GENERATORS_WEB_API_PROJECT || ANALYZERS_NONFRAMEWORK || GENERATORS_WORKERS_PROJECT
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
#endif
#if COMMON_PROJECT
    /// <summary>
    ///     Returns a string value for all the items in the list, separated by the specified <see cref="orKeyword" />
    /// </summary>
    public static string JoinAsOredChoices(this IEnumerable<string> collection, string orKeyword = ",")
    {
        return collection.Join($"{orKeyword} ");
    }

    /// <summary>
    ///     Returns the last item in the collection
    /// </summary>
    public static TResult Last<TResult>(this IReadOnlyList<TResult> collection)
    {
        return collection[^1];
    }
#endif
}