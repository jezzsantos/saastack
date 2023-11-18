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
            .ForEach(entry => { source.TryAdd(entry.Key, entry.Value); });
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
}