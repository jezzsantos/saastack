using AutoMapper;

namespace Common.Extensions;

public static class DictionaryExtensions
{
    /// <summary>
    ///     Constructs a new instance of the <see cref="TObject" /> with the <see cref="values" />
    /// </summary>
    public static TObject FromObjectDictionary<TObject>(this IReadOnlyDictionary<string, object?> values)
    {
        var configuration = new MapperConfiguration(_ => { });
        var mapper = configuration.CreateMapper();

        return mapper.Map<TObject>(values);
    }

    /// <summary>
    ///     Merges the values from <see cref="other" /> into the existing values of <see cref="source" />,
    ///     Where there are not duplications
    /// </summary>
    public static void Merge<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> other)
        where TKey : notnull
    {
        other.ToList()
            .ForEach(entry => { source[entry.Key] = entry.Value; });
    }

    /// <summary>
    ///     Converts the instance of the <see cref="TObject" /> to a <see cref="IReadOnlyDictionary{String,Object}" />
    /// </summary>
    public static IReadOnlyDictionary<string, object?> ToObjectDictionary<TObject>(this TObject instance)
    {
        var configuration = new MapperConfiguration(_ => { });
        var mapper = configuration.CreateMapper();

        return mapper.Map<Dictionary<string, object?>>(instance);
    }

    /// <summary>
    ///     Converts the instance of the <see cref="TObject" /> to a <see cref="IReadOnlyDictionary{String,String}" />,
    ///     where null properties are removed
    /// </summary>
    public static IReadOnlyDictionary<string, string> ToStringDictionary<TObject>(this TObject instance)
    {
        var objectJson = instance.ToJson(false) ?? "{}";

        var properties = objectJson.FromJson<Dictionary<string, object?>>();
        if (properties.NotExists())
        {
            return new Dictionary<string, string>();
        }

        return properties
            .Where(entry => entry.Value.Exists() && entry.Value.ToString().Exists())
            .ToDictionary(entry => entry.Key, entry => entry.Value!.ToString()!);
    }

    /// <summary>
    ///     Adds the result of the specified <see cref="converter" /> to the dictionary, only if the specified
    ///     <see cref="condition" />
    ///     resolves to true
    /// </summary>
    public static void TryAddIfTrue<TExpression, TKey, TValue>(this Dictionary<TKey, TValue> dictionary,
        TKey key,
        TExpression value,
        Predicate<TExpression> condition, Func<TExpression, TValue> converter)
        where TKey : notnull
    {
        var isTrue = condition(value);
        if (isTrue)
        {
            dictionary.TryAdd(key, converter(value));
        }
    }

    /// <summary>
    ///     Adds the specified <see cref="value" /> to the dictionary, only if the specified <see cref="condition" />
    ///     resolves to true
    /// </summary>
    public static void TryAddIfTrue<TKey, TValue>(this Dictionary<TKey, TValue> dictionary,
        TKey key, TValue value, Predicate<TValue> condition)
        where TKey : notnull
    {
        var isTrue = condition(value);
        if (isTrue)
        {
            dictionary.TryAdd(key, value);
        }
    }
}