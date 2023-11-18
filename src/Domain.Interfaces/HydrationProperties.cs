using System.Collections.ObjectModel;
using Common;
using Common.Extensions;

namespace Domain.Interfaces;

/// <summary>
///     Defines a set of properties used for the dehydration and rehydration of types
/// </summary>
public class HydrationProperties : ReadOnlyDictionary<string, Optional<object>>
{
    public HydrationProperties() : base(new Dictionary<string, Optional<object>>())
    {
    }

    public HydrationProperties(IDictionary<string, Optional<object>> dictionary) : base(dictionary)
    {
    }

    public HydrationProperties(IReadOnlyDictionary<string, object?> dictionary) : base(
        dictionary.ToDictionary(pair => pair.Key, pair =>
        {
            var value = pair.Value;
            if (value is null)
            {
                return Optional<object>.None;
            }

            if (value.IsOptional(out var contained))
            {
                return contained.ToOptional();
            }

            return value.ToOptional();
        }))
    {
    }

    public void Add<TValue>(string name, TValue value)
    {
        AddOrUpdate(name, new Optional<TValue>(value));
    }

    public void Add<TValue>(string name, Optional<TValue> value)
    {
        AddOrUpdate(name, value);
    }

    public void AddOrUpdate<TValue>(string name, Optional<TValue> value)
    {
        var optional = new Optional<object>((object?)value.ValueOrDefault);
        if (!Dictionary.TryAdd(name, optional))
        {
            Dictionary[name] = optional;
        }
    }

    public static HydrationProperties FromDto(object? instance)
    {
        if (instance is null)
        {
            return new HydrationProperties();
        }

        var properties = instance.ToObjectDictionary();
        return new HydrationProperties(properties);
    }

    public TDto ToDto<TDto>()
    {
        return Dictionary
            .ToDictionary(pair => pair.Key, pair => pair.Value.ValueOrDefault)
            .FromObjectDictionary<TDto>();
    }

    public IReadOnlyDictionary<string, object?> ToObjectDictionary()
    {
        return Dictionary.ToDictionary(pair => pair.Key, pair => pair.Value.ValueOrDefault);
    }
}