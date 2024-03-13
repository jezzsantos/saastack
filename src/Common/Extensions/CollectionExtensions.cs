#if COMMON_PROJECT || ANALYZERS_NONPLATFORM
using System.Text;
#endif

#if COMMON_PROJECT
using JetBrains.Annotations;
#endif

namespace Common.Extensions;

#if COMMON_PROJECT
[UsedImplicitly]
#endif

public static class CollectionExtensions
{
#if COMMON_PROJECT || GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM
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
#if COMMON_PROJECT || ANALYZERS_NONPLATFORM
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
#endif
#if COMMON_PROJECT || ANALYZERS_NONPLATFORM
    /// <summary>
    ///     Whether the <see cref="target" /> string does not exist in the <see cref="collection" />
    /// </summary>
    public static bool NotContainsIgnoreCase(this IEnumerable<string> collection, string target)
    {
        return !collection.ContainsIgnoreCase(target);
    }
#endif
#if COMMON_PROJECT || GENERATORS_WEB_API_PROJECT || ANALYZERS_NONPLATFORM
    /// <summary>
    ///     Whether the collection contains any items
    /// </summary>
    public static bool HasAny<T>(this IEnumerable<T>? collection)
    {
        if (collection.NotExists())
        {
            return false;
        }
#if COMMON_PROJECT || ANALYZERS_NONPLATFORM
        return !collection.HasNone();
#elif GENERATORS_WEB_API_PROJECT
        return !collection!.HasNone();
#endif
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
    public static string JoinAsOredChoices(this IEnumerable<string> list, string orKeyword = ",")
    {
        return list.Join($"{orKeyword} ");
    }

    /// <summary>
    ///     Returns the last item in the collection
    /// </summary>
    public static TResult Last<TResult>(this IReadOnlyList<TResult> list)
    {
        return list[^1];
    }
#endif
}