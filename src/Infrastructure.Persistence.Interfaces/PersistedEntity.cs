using Common;
using Common.Extensions;
using Domain.Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;

namespace Infrastructure.Persistence.Interfaces;

/// <summary>
///     Defines an entity that is persisted
/// </summary>
public abstract class PersistedEntity
{
    protected readonly HydrationProperties PropertyValues = new();

    protected PersistedEntity() : this(Optional<string>.None)
    {
    }

    protected PersistedEntity(Optional<string> id)
    {
        Metadata = new PersistedEntityMetadata();
        Add(nameof(Id), id);
        Add(nameof(LastPersistedAtUtc), Optional<DateTime>.None);
        Add(nameof(IsDeleted), Optional<bool>.None);
    }

    public Optional<string> Id
    {
        get => PropertyValues.GetValueOrDefault<string>(nameof(Id));
        set => PropertyValues.AddOrUpdate(nameof(Id), value);
    }

    public Optional<bool> IsDeleted
    {
        get => PropertyValues.GetValueOrDefault<bool>(nameof(IsDeleted));
        set => PropertyValues.AddOrUpdate(nameof(IsDeleted), value);
    }

    public Optional<DateTime> LastPersistedAtUtc
    {
        get => PropertyValues.GetValueOrDefault<DateTime>(nameof(LastPersistedAtUtc));
        set => PropertyValues.AddOrUpdate(nameof(LastPersistedAtUtc), value);
    }

    public PersistedEntityMetadata Metadata { get; }

    public HydrationProperties Properties => PropertyValues;

    public void Add<TValue>(string name, TValue value)
    {
        Add(name, new Optional<TValue>(value), typeof(TValue));
    }

    public void Add<TValue>(string name, Optional<TValue> value)
    {
        var typeOfValue = typeof(TValue);
        var isValueType = typeOfValue.IsValueType;
        var type = value.HasValue
            ? typeOfValue
            : isValueType
                ? MakeNullable(typeOfValue)
                : typeOfValue;
        Add(name, value, type);
        return;

        static Type MakeNullable(Type type)
        {
            return typeof(Nullable<>).MakeGenericType(type);
        }
    }

    public TValue? GetValueOrDefault<TValue>(string propertyName, TValue? defaultValue = default)
    {
        if (!PropertyValues.ContainsKey(propertyName))
        {
            return defaultValue;
        }

        var propertyValue = ConvertToDomainProperty(PropertyValues[propertyName], typeof(TValue));
        if (!propertyValue.HasValue)
        {
            return defaultValue;
        }

        return propertyValue.Value is TValue value
            ? value
            : defaultValue;
    }

    public TValue? GetValueOrDefault<TValue>(string propertyName, IDomainFactory domainFactory)
    {
        if (!PropertyValues.ContainsKey(propertyName))
        {
            return default;
        }

        var propertyValue = ConvertToDomainProperty(PropertyValues[propertyName], typeof(TValue), domainFactory);
        if (!propertyValue.HasValue)
        {
            return default;
        }

        return propertyValue.Value is TValue value
            ? value
            : default;
    }

    protected void Add<TValue>(string name, Optional<TValue> value, Type type)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);

        var rawValue = ConvertFromDomainProperty(value);
        if (PropertyValues.ContainsKey(name))
        {
            PropertyValues.AddOrUpdate(name, rawValue);
            Metadata.AddOrUpdate(name, type);
        }
        else
        {
            PropertyValues.AddOrUpdate(name, rawValue);
            Metadata.AddOrUpdate(name, type);
        }
    }

    protected HydrationProperties ConvertToDomainProperties(IDomainFactory domainFactory)
    {
        var properties = PropertyValues.ToDictionary(pair => pair.Key, pair =>
        {
            var value = pair.Value;
            var propertyType = GetPropertyType(pair.Key);
            if (!propertyType.HasValue)
            {
                return new Optional<object>((object?)default);
            }

            var domainProperty =
                ConvertToDomainProperty(value, propertyType.Value, domainFactory);
            if (!domainProperty.HasValue)
            {
                return Optional<object>.None;
            }

            return new Optional<object>(domainProperty.Value);
        });

        return new HydrationProperties(properties);
    }

    private Optional<Type> GetPropertyType(string propertyName)
    {
        return Metadata.GetPropertyType(propertyName);
    }

    private static Optional<object> ConvertFromDomainProperty<TValue>(Optional<TValue> value)
    {
        if (!value.HasValue)
        {
            return Optional<object>.None;
        }

        if (value.Value is IDehydratableValueObject valueObject)
        {
            return new Optional<object>((object?)valueObject.Dehydrate());
        }

        return value.ToOptional<object>();
    }

    private static Optional<object> ConvertToDomainProperty(Optional<object> rawValue, Type propertyType,
        IDomainFactory? domainFactory = null)
    {
        if (Optional.IsOptionalType(propertyType)
            && Optional.TryGetContainedType(propertyType, out var containedType))
        {
            if (containedType!.IsAssignableTo(typeof(IDehydratableValueObject)))
            {
                if (!rawValue.HasValue)
                {
                    return Optional<object>.None;
                }

                if (domainFactory.NotExists())
                {
                    return Optional<object>.None;
                }

                var valueObject = domainFactory.RehydrateValueObject(propertyType, (string)rawValue.Value);
                return new Optional<object>(valueObject.ToOptional(containedType));
            }

            var optional = Optional.ChangeType(rawValue, containedType);
            return new Optional<object>(optional);
        }

        if (typeof(IDehydratableValueObject).IsAssignableFrom(propertyType))
        {
            if (!rawValue.HasValue)
            {
                return Optional<object>.None;
            }

            if (domainFactory.NotExists())
            {
                return Optional<object>.None;
            }

            return new Optional<object>(domainFactory.RehydrateValueObject(propertyType, (string)rawValue.Value));
        }

        return rawValue;
    }
}