namespace Common.Extensions;

public static class DictionaryExtensions
{
    /// <summary>
    ///     Merges the values from <see cref="other" /> into the existing values of <see cref="source" />,
    ///     Where there are not duplications
    /// </summary>
    public static void Merge<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> other)
        where TKey : notnull
    {
        other.ToList().ForEach(entry => { source.TryAdd(entry.Key, entry.Value); });
    }
}