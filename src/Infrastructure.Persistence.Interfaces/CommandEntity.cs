using Application.Persistence.Interfaces;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using QueryAny;

namespace Infrastructure.Persistence.Interfaces;

/// <summary>
///     Defines an entity used in persistence of [CQRS] commands
/// </summary>
public sealed class CommandEntity : PersistedEntity, IHasIdentity
{
    public CommandEntity(string id) : base(id)
    {
    }

    /// <summary>
    ///     Converts the given <see cref="properties" /> with the given <see cref="descriptor" /> into a new entity
    /// </summary>
    public static CommandEntity FromHydrationProperties(HydrationProperties properties,
        CommandEntity descriptor)
    {
        return FromHydrationProperties(properties, descriptor.Metadata);
    }

    /// <summary>
    ///     Converts the given <see cref="properties" /> with the given <see cref="metadata" /> into a new entity
    /// </summary>
    public static CommandEntity FromHydrationProperties(HydrationProperties properties,
        PersistedEntityMetadata metadata)
    {
        return FromProperties(properties, metadata);
    }

    /// <summary>
    ///     Converts the given DDD <see cref="entity" /> into a new entity
    /// </summary>
    public static CommandEntity FromDomainEntity<TEntity>(TEntity entity)
        where TEntity : IDehydratableEntity
    {
        var properties = entity.Dehydrate();
        return FromProperties<TEntity>(properties);
    }

    /// <summary>
    ///     Converts the given <see cref="dto" /> into a new entity
    /// </summary>
    public static CommandEntity FromDto<TDto>(TDto dto)
        where TDto : IQueryableEntity, IHasIdentity, new()
    {
        return FromType(dto);
    }

    /// <summary>
    ///     Converts the given <see cref="dto" /> into a new entity
    /// </summary>
    public static CommandEntity FromType<TType>(TType instance)
        where TType : IQueryableEntity, IHasIdentity
    {
        var properties = HydrationProperties.FromDto(instance);
        return FromProperties<TType>(properties);
    }

    /// <summary>
    ///     Creates a new instance of the <see cref="TDomainEntity" />, populated with the data in this entity.
    /// </summary>
    public TDomainEntity ToDomainEntity<TDomainEntity>(IDomainFactory domainFactory)
        where TDomainEntity : IDehydratableEntity
    {
        var domainProperties = ConvertToDomainProperties(domainFactory);
        if (typeof(TDomainEntity).IsAssignableTo(typeof(IDehydratableAggregateRoot)))
        {
            var result = domainFactory.RehydrateAggregateRoot(typeof(TDomainEntity), domainProperties);
            return (TDomainEntity)result;
        }
        else
        {
            var result = domainFactory.RehydrateEntity(typeof(TDomainEntity), domainProperties);
            return (TDomainEntity)result;
        }
    }

    /// <summary>
    ///     Creates a new instance of the <see cref="TDto" />, populated with the data in this entity.
    ///     <see cref="TDto" /> cannot contain any domain objects
    /// </summary>
    public TDto ToDto<TDto>()
        where TDto : IQueryableEntity, IHasIdentity, new()
    {
        return Properties.ToDto<TDto>();
    }

    /// <summary>
    ///     Creates a new instance of the <see cref="TDomainEntity" />, populated with the data in this entity.
    /// </summary>
    public TDomainEntity ToQueryEntity<TDomainEntity>(IDomainFactory domainFactory)
        where TDomainEntity : IQueryableEntity, IHasIdentity, new()
    {
        var properties = ConvertToDomainProperties(domainFactory);
        return properties.ToDto<TDomainEntity>();
    }

    /// <summary>
    ///     Creates a new instance of the <see cref="TReadModelEntity" />, populated with the data in this entity.
    /// </summary>
    public TReadModelEntity ToReadModelDto<TReadModelEntity>(IDomainFactory domainFactory)
        where TReadModelEntity : IReadModelEntity, new()
    {
        var properties = ConvertToDomainProperties(domainFactory);
        return properties.ToDto<TReadModelEntity>();
    }

    private static CommandEntity FromProperties(HydrationProperties properties,
        PersistedEntityMetadata metadata)
    {
        if (!properties.ContainsKey(nameof(Id))
            || !properties[nameof(Id)].HasValue)
        {
            throw new InvalidOperationException(
                Resources.CommandEntity_FromProperties_NoId);
        }

        var result = new CommandEntity(properties[nameof(Id)].Value.ToString()!);

        foreach (var property in properties)
        {
            var propertyType = metadata.GetPropertyType(property.Key, false);
            if (propertyType.HasValue)
            {
                result.Add(property.Key, property.Value, propertyType.Value);
            }
        }

        return result;
    }

    private static CommandEntity FromProperties<TType>(HydrationProperties properties)
        where TType : IQueryableEntity
    {
        var metadata = PersistedEntityMetadata.FromType<TType>();

        return FromProperties(properties, metadata);
    }
}