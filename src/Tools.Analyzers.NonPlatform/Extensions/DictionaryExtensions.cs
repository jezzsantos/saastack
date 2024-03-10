// ReSharper disable once CheckNamespace

namespace Common.Extensions;

public static class DictionaryExtensions2
{
    /// <summary>
    ///     Adds the item to the dictionary if the key does not exist
    /// </summary>
    public static bool TryAdd<TKey, TValue>(this IDictionary<TKey, TValue> values, TKey key, TValue value)
    {
        if (values.ContainsKey(key))
        {
            return false;
        }

        values.Add(key, value);

        return true;
    }
}