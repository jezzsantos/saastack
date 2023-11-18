using Domain.Interfaces;
using QueryAny;

namespace Infrastructure.Persistence.Interfaces;

/// <summary>
///     Defines an entity used in persistence of [CQRS] queries
/// </summary>
public sealed class QueryEntity : PersistedEntity, IQueryableEntity
{
    /// <summary>
    ///     Converts the given <see cref="properties" /> with the given <see cref="metadata" /> into a new entity
    /// </summary>
    public static QueryEntity FromProperties(HydrationProperties properties, PersistedEntityMetadata metadata)
    {
        var entity = new QueryEntity();
        foreach (var pair in properties)
        {
            var propertyType = metadata.GetPropertyType(pair.Key, false);
            if (propertyType.HasValue)
            {
                entity.Add(pair.Key, pair.Value, propertyType.Value);
            }
        }

        return entity;
    }

    /// <summary>
    ///     Converts the given <see cref="properties" /> with the given <see cref="metadata" /> into a new entity
    /// </summary>
    public static QueryEntity FromQueryEntity(HydrationProperties properties, PersistedEntityMetadata metadata)
    {
        return FromProperties(properties, metadata);
    }

    /// <summary>
    ///     Converts the given <see cref="instance" /> to a new entity
    /// </summary>
    public static QueryEntity FromType<TType>(TType instance)
        where TType : IQueryableEntity
    {
        var properties = HydrationProperties.FromDto(instance);
        return FromProperties<TType>(properties);
    }

    /// <summary>
    ///     Converts this entity to a new instance of the <see cref="TDomainEntity" />, populated with the data in this entity.
    /// </summary>
    public TDomainEntity ToDomainEntity<TDomainEntity>(IDomainFactory domainFactory)
        where TDomainEntity : IQueryableEntity, new()
    {
        var properties = ConvertToDomainProperties(domainFactory);
        return properties.ToDto<TDomainEntity>();
    }

    /// <summary>
    ///     Converts this entity to a new instance of the <see cref="TDto" />, populated with the data in this entity.
    ///     Note: <see cref="TDto" /> cannot be a domain aggregate/entity or contain any value objects,
    ///     since conversion requires no <see cref="IDomainFactory" />
    /// </summary>
    public TDto ToDto<TDto>()
        where TDto : IQueryableEntity, new()
    {
        return Properties.ToDto<TDto>();
    }

    private static QueryEntity FromProperties<TType>(HydrationProperties properties)
        where TType : IQueryableEntity
    {
        var metadata = PersistedEntityMetadata.FromType<TType>();

        return FromProperties(properties, metadata);
    }
}