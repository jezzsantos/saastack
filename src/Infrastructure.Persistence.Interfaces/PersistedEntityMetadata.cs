using System.Reflection;
using Common;
using Common.Extensions;
using Domain.Interfaces.Entities;

namespace Infrastructure.Persistence.Interfaces;

/// <summary>
///     Defines the metadata of a persisted entity
/// </summary>
public class PersistedEntityMetadata
{
    private const string DefaultOrderingFieldMethodName = "DefaultOrderingField";
    private const string FieldReadMappingsMethodName = "FieldReadMappings";
    private static readonly Dictionary<Type, string?> DefaultOrderingsCache = new();
    private static readonly Dictionary<Type, Dictionary<string, Func<IReadOnlyDictionary<string, object?>, object?>>>
        FieldReadMappingsCache = new();
    private static readonly Dictionary<Type, IEnumerable<PropertyInfo>> TypePropertiesCache = new();
    private readonly Dictionary<string, Type> _propertyTypes;

    internal PersistedEntityMetadata(Type? type = null, Dictionary<string, Type>? propertyTypes = null)
    {
        UnderlyingType = type ?? typeof(IDehydratableEntity);
        _propertyTypes = propertyTypes ?? new Dictionary<string, Type>();
    }

    public static PersistedEntityMetadata Empty => new();

    public IReadOnlyDictionary<string, Type> Types => _propertyTypes;

    internal Type UnderlyingType { get; }

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
        return new PersistedEntityMetadata(type, properties);
    }

    /// <summary>
    ///     Returns the overridden default ordering field for the source data of this entity.
    ///     Searches for an optional static parameterless method called <see cref="DefaultOrderingFieldMethodName" />
    ///     on the entity class, with a specific return type, that contains one function that calculates the sorting field
    /// </summary>
    public string? GetDefaultOrderingFieldOverride()
    {
        return FromCache(UnderlyingType, GetOverride());

        string? GetOverride()
        {
            var method =
                UnderlyingType.GetMethod(DefaultOrderingFieldMethodName, BindingFlags.Public | BindingFlags.Static);
            if (method.NotExists())
            {
                return null;
            }

            try
            {
                var sortBy = method.Invoke(UnderlyingType, null);
                if (sortBy is string fieldName)
                {
                    return fieldName;
                }
            }
            catch (Exception ex) when (ex is TargetException or TargetInvocationException or ArgumentException)
            {
                return null;
            }

            return null;
        }
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

    /// <summary>
    ///     Returns the overridden collection of mappings for translating the source data from the store to the data of this
    ///     entity.
    ///     Searches for an optional static parameterless method called <see cref="FieldReadMappingsMethodName" />
    ///     on the entity class, with a specific return type, that contains one or more mapping functions
    /// </summary>
    public IReadOnlyDictionary<string, Func<IReadOnlyDictionary<string, object?>, object?>> GetReadMappingsOverride()
    {
        return FromCache(UnderlyingType, GetOverride());

        Dictionary<string, Func<IReadOnlyDictionary<string, object?>, object?>> GetOverride()
        {
            var method =
                UnderlyingType.GetMethod(FieldReadMappingsMethodName, BindingFlags.Public | BindingFlags.Static);
            if (method.NotExists())
            {
                return new Dictionary<string, Func<IReadOnlyDictionary<string, object?>, object?>>();
            }

            try
            {
                var mappings = method.Invoke(UnderlyingType, null);
                if (mappings is Dictionary<string, Func<IReadOnlyDictionary<string, object?>, object?>> dictionary)
                {
                    return dictionary;
                }
            }
            catch (Exception ex) when (ex is TargetException or TargetInvocationException or ArgumentException)
            {
                return new Dictionary<string, Func<IReadOnlyDictionary<string, object?>, object?>>();
            }

            return new Dictionary<string, Func<IReadOnlyDictionary<string, object?>, object?>>();
        }
    }

    public bool HasType(string propertyName)
    {
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        return _propertyTypes.ContainsKey(propertyName);
    }

    private static string? FromCache(Type underlyingType, string? defaultOrdering)
    {
        if (!DefaultOrderingsCache.ContainsKey(underlyingType))
        {
            DefaultOrderingsCache[underlyingType] = defaultOrdering;
        }

        return DefaultOrderingsCache[underlyingType];
    }

    private static IReadOnlyDictionary<string, Func<IReadOnlyDictionary<string, object?>, object?>> FromCache(
        Type underlyingType, Dictionary<string, Func<IReadOnlyDictionary<string, object?>, object?>> mappings)
    {
        if (!FieldReadMappingsCache.ContainsKey(underlyingType))
        {
            FieldReadMappingsCache[underlyingType] = mappings;
        }

        return FieldReadMappingsCache[underlyingType];
    }

    private static Dictionary<string, Type> GetProperties(Type type)
    {
        var properties = FromCache(type, t => t.GetProperties());

        return properties.ToDictionary(prop => prop.Name, prop => prop.PropertyType);
    }

    private static IEnumerable<PropertyInfo> FromCache(Type type, Func<Type, PropertyInfo[]> propertyInfoFactory)
    {
        if (!TypePropertiesCache.ContainsKey(type))
        {
            TypePropertiesCache[type] = propertyInfoFactory(type);
        }

        return TypePropertiesCache[type];
    }
}