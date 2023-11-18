using System.Reflection;
using Common;
using Common.Extensions;

namespace Infrastructure.Persistence.Interfaces;

/// <summary>
///     Defines the metadata of a persisted entity
/// </summary>
public class PersistedEntityMetadata
{
    private static readonly Dictionary<Type, IEnumerable<PropertyInfo>> TypePropertiesCache = new();

    private readonly Dictionary<string, Type> _propertyTypes;

    internal PersistedEntityMetadata(Dictionary<string, Type>? propertyTypes = null)
    {
        _propertyTypes = propertyTypes ?? new Dictionary<string, Type>();
    }

    public static PersistedEntityMetadata Empty => new();

    public IReadOnlyDictionary<string, Type> Types => _propertyTypes;

    public void AddOrUpdate(string propertyName, Type type)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        _propertyTypes[propertyName] = type;
    }

    public static PersistedEntityMetadata FromType<TType>()
    {
        return FromType(typeof(TType));
    }

    public static PersistedEntityMetadata FromType(Type type)
    {
        var properties = GetProperties(type);
        return new PersistedEntityMetadata(properties);
    }

    public Optional<Type> GetPropertyType(string propertyName, bool throwIfNotExists = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        if (_propertyTypes.TryGetValue(propertyName, out var value))
        {
            return value.ToOptional();
        }

        if (!throwIfNotExists)
        {
            return Optional<Type>.None;
        }

        throw new InvalidOperationException(
            Resources.PersistedEntityMetadata_GetPropertyType_NoTypeForProperty.Format(propertyName));
    }

    public bool HasType(string propertyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        return _propertyTypes.ContainsKey(propertyName);
    }

    private static Dictionary<string, Type> GetProperties(Type type)
    {
        var properties = WithCache(type, t => t.GetProperties());

        return properties.ToDictionary(prop => prop.Name, prop => prop.PropertyType);
    }

    private static IEnumerable<PropertyInfo> WithCache(Type type, Func<Type, PropertyInfo[]> propertyInfoFactory)
    {
        if (!TypePropertiesCache.ContainsKey(type))
        {
            TypePropertiesCache[type] = propertyInfoFactory(type);
        }

        return TypePropertiesCache[type];
    }
}