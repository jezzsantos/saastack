namespace Common.Extensions;

public static class CollectionExtensions
{
    public static bool HasAny<T>(this IEnumerable<T> collection)
    {
        return !collection.HasNone();
    }

    public static bool HasNone<T>(this IEnumerable<T> collection)
    {
        return !collection.Any();
    }
}