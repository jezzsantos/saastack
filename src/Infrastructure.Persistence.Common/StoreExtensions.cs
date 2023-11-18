using System.Linq.Dynamic.Core;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.Persistence.Common;

public static class StoreExtensions
{
    public const string BackupOrderingPropertyName = nameof(QueryEntity.Id);
    public const string DefaultOrderingPropertyName = nameof(QueryEntity.LastPersistedAtUtc);

    /// <summary>
    ///     Converts the JSON serialized <see cref="propertyValue" /> to a value the appropriate
    ///     <see cref="targetPropertyType" />
    /// </summary>
    public static Optional<object> ComplexTypeFromContainerProperty(this Optional<string> propertyValue,
        Type targetPropertyType)
    {
        if (propertyValue.HasValue
            && propertyValue.Value.HasValue())
        {
            if (!targetPropertyType.IsComplexStorageType())
            {
                return propertyValue;
            }

            try
            {
                if (propertyValue.Value.StartsWith("{") && propertyValue.Value.EndsWith("}"))
                {
                    return new Optional<object>(propertyValue.Value.FromJson(targetPropertyType));
                }
            }
            catch (Exception)
            {
                return Optional<object>.None;
            }
        }

        return Optional<object>.None;
    }

    /// <summary>
    ///     Converts the <see cref="propertyValue" /> to a JSON serializable value
    /// </summary>
    public static Optional<string> ComplexTypeToContainerProperty<TValue>(this Optional<TValue> propertyValue)
    {
        if (!propertyValue.HasValue)
        {
            return Optional<string>.None;
        }

        var propertyType = typeof(TValue);
        if (!propertyType.IsComplexStorageType())
        {
            return new Optional<string>(propertyValue.ValueOrDefault?.ToString());
        }

        if (HasToStringMethodBeenOverridenInSubclass(propertyType))
        {
            return new Optional<string>(propertyValue.ValueOrDefault?.ToString());
        }

        return new Optional<string>(
            propertyValue.ValueOrDefault?.ToJson(false, StringExtensions.JsonCasing.Pascal));
    }

    /// <summary>
    ///     Returns a list of <see cref="QueryEntity" /> from the specified <see cref="store" /> for the specified
    ///     <see cref="query" />.
    ///     This operation will perform the joins (if any) in memory.
    ///     HACK: this operation is not recommended for use in large production workloads
    /// </summary>
    public static List<QueryEntity> FetchAllIntoMemory<TQueryableEntity>(this QueryClause<TQueryableEntity> query,
        int maxQueryResults, PersistedEntityMetadata metadata,
        Func<Dictionary<string, HydrationProperties>> getPrimaryEntities,
        Func<QueriedEntity, Dictionary<string, HydrationProperties>> getJoinedEntities)
        where TQueryableEntity : IQueryableEntity
    {
        var take = query.GetDefaultTake(maxQueryResults);
        if (take == 0)
        {
            return new List<QueryEntity>();
        }

        var primaryEntities = getPrimaryEntities();
        if (!primaryEntities.HasAny())
        {
            return new List<QueryEntity>();
        }

        var joinedContainers = query.JoinedEntities
            .Where(je => je.Join.Exists())
            .ToDictionary(je => je.EntityName, je => new
            {
                Collection = getJoinedEntities(je),
                JoinedEntity = je
            });

        var joinedEntities = new List<KeyValuePair<string, HydrationProperties>>();
        if (!joinedContainers.Any())
        {
            joinedEntities = primaryEntities
                .Select(pe => new KeyValuePair<string, HydrationProperties>(pe.Key, pe.Value))
                .ToList();
        }
        else
        {
            foreach (var joinedContainer in joinedContainers.Select(jc => jc.Value))
            {
                var joinedEntity = joinedContainer.JoinedEntity;
                var join = joinedEntity.Join;
                var rightEntities = joinedContainer.Collection
                    .ToDictionary(e => e.Key, e => e.Value);

                joinedEntities = join
                    .JoinResults(primaryEntities, rightEntities,
                        joinedEntity.Selects.ProjectSelectedJoinedProperties());
            }
        }

        var results = joinedEntities
            .ToDictionary(pair => pair.Key, pair => pair.Value.ToObjectDictionary())
            .AsQueryable();
        var orderBy = query.ToDynamicLinqOrderByClause();
        var skip = query.GetDefaultSkip();

        if (query.Wheres.Any())
        {
            var whereBy = query.Wheres.ToDynamicLinqWhereClause();
            results = results
                .Where(whereBy);
        }

        return results
            .OrderBy(orderBy)
            .Skip(skip)
            .Take(take)
            .Select(sel => new KeyValuePair<string, HydrationProperties>(sel.Key, new HydrationProperties(sel.Value)))
            .CherryPickSelectedProperties(query)
            .Select(ped => QueryEntity.FromProperties(ped.Value, metadata))
            .ToList();
    }

    /// <summary>
    ///     Returns the default ordering field for the specified <see cref="query" />.
    ///     Order of precedence:
    ///     1. <see cref="QueryClause{TPrimaryEntity}.ResultOptions.OrderBy" />
    ///     2. If selected in query, <see cref="QueryEntity.LastPersistedAtUtc" />
    ///     3. If selected in query, <see cref="QueryEntity.Id" />
    ///     4. First of the <see cref="QueryClause{TPrimaryEntity}.Select{TValue}" />
    /// </summary>
    public static string GetDefaultOrdering<TQueryableEntity>(this QueryClause<TQueryableEntity> query)
        where TQueryableEntity : IQueryableEntity
    {
        var by = query.ResultOptions.OrderBy.By;
        if (by.HasNoValue())
        {
            by = DefaultOrderingPropertyName;
        }

        var selectedFields = query.GetAllSelectedFields();
        if (selectedFields.Any())
        {
            if (selectedFields.Contains(by))
            {
                return by;
            }

            return selectedFields.Contains(BackupOrderingPropertyName)
                ? BackupOrderingPropertyName
                : Enumerable.First(selectedFields);
        }

        if (HasProperty<TQueryableEntity>(by))
        {
            return by;
        }

        return HasProperty<TQueryableEntity>(BackupOrderingPropertyName)
            ? BackupOrderingPropertyName
            : FirstProperty<TQueryableEntity>();
    }

    /// <summary>
    ///     Returns the default skip total for the specified <see cref="query" />
    /// </summary>
    public static int GetDefaultSkip<TQueryableEntity>(this QueryClause<TQueryableEntity> query)
        where TQueryableEntity : IQueryableEntity
    {
        return query.ResultOptions.Offset != ResultOptions.DefaultOffset
            ? query.ResultOptions.Offset
            : 0;
    }

    /// <summary>
    ///     Returns the default take total for the specified <see cref="query" />
    /// </summary>
    public static int GetDefaultTake<TQueryableEntity>(this QueryClause<TQueryableEntity> query, int maxResults)
        where TQueryableEntity : IQueryableEntity
    {
        return query.ResultOptions.Limit == ResultOptions.DefaultLimit
            ? maxResults
            : query.ResultOptions.Limit;
    }

    private static List<KeyValuePair<string, HydrationProperties>> JoinResults(
        this JoinDefinition joinDefinition,
        IReadOnlyDictionary<string, HydrationProperties> leftEntities,
        IReadOnlyDictionary<string, HydrationProperties> rightEntities,
        Func<KeyValuePair<string, HydrationProperties>,
            KeyValuePair<string, HydrationProperties>,
            KeyValuePair<string, HydrationProperties>>? mapFunc = null)
    {
        switch (joinDefinition.Type)
        {
            case JoinType.Inner:
                var innerJoin = from lefts in leftEntities
                    join rights in rightEntities on lefts.Value[joinDefinition.Left.JoinedFieldName] equals
                        rights.Value[joinDefinition.Right.JoinedFieldName]
                        into joined
                    from result in joined
                    select mapFunc?.Invoke(lefts, result) ?? lefts;

                return innerJoin
                    .Select(join =>
                        new KeyValuePair<string, HydrationProperties>(join.Key, join.Value))
                    .ToList();

            case JoinType.Left:
                var leftJoin = from lefts in leftEntities
                    join rights in rightEntities on lefts.Value[joinDefinition.Left.JoinedFieldName] equals
                        rights.Value[joinDefinition.Right.JoinedFieldName]
                        into joined
                    from result in joined.DefaultIfEmpty()
                    select mapFunc?.Invoke(lefts, result) ?? lefts;

                return leftJoin
                    .Select(join =>
                        new KeyValuePair<string, HydrationProperties>(join.Key, join.Value))
                    .ToList();

            default:
                throw new ArgumentOutOfRangeException(
                    Resources.StoreExtensions_InvalidJoinType.Format(joinDefinition.Type));
        }
    }

    private static Func<KeyValuePair<string, HydrationProperties>,
            KeyValuePair<string, HydrationProperties>,
            KeyValuePair<string, HydrationProperties>>
        ProjectSelectedJoinedProperties(this IReadOnlyList<SelectDefinition> selects)
    {
        return (leftEntity, rightEntity) =>
        {
            var selectedFromJoinPropertyNames = selects
                .Where(x => x.JoinedFieldName.HasValue())
                .ToList();
            if (!selectedFromJoinPropertyNames.Any())
            {
                return leftEntity;
            }

            var leftEntityProperties = leftEntity.Value;
            var rightEntityProperties = rightEntity.Value;
            foreach (var select in selectedFromJoinPropertyNames)
            {
                if (!rightEntityProperties.HasPropertyValue(select.FieldName))
                {
                    continue;
                }

                leftEntityProperties.CopyPropertyValue(rightEntityProperties, select);
            }

            return leftEntity;
        };
    }

    private static IEnumerable<KeyValuePair<string, HydrationProperties>>
        CherryPickSelectedProperties<
            TQueryableEntity>(this IQueryable<KeyValuePair<string, HydrationProperties>> entities,
            QueryClause<TQueryableEntity> query)
        where TQueryableEntity : IQueryableEntity
    {
        var selectedPropertyNames = query.GetAllSelectedFields();

        if (!selectedPropertyNames.Any())
        {
            return entities;
        }

        selectedPropertyNames = selectedPropertyNames
            .Concat(new[] { nameof(QueryEntity.Id) })
            .ToList();

        return entities
            .Select(entity => FilterSelectedFields(entity, selectedPropertyNames))
            .Select(sel => new KeyValuePair<string, HydrationProperties>(sel.Key, sel.Value));
    }

    private static KeyValuePair<string, HydrationProperties> FilterSelectedFields(
        KeyValuePair<string, HydrationProperties> entity, List<string> allowedFieldNames)
    {
        return new KeyValuePair<string, HydrationProperties>(entity.Key,
            new HydrationProperties(entity.Value.Where(fieldNameValue => allowedFieldNames.Contains(fieldNameValue.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value)));
    }

    private static List<string> GetAllSelectedFields<TQueryableEntity>(this QueryClause<TQueryableEntity> query)
        where TQueryableEntity : IQueryableEntity

    {
        var primarySelects = query.PrimaryEntity.Selects;
        var joinedSelects = query.JoinedEntities.SelectMany(je => je.Selects);

        return primarySelects
            .Select(select => select.FieldName)
            .Concat(joinedSelects.Select(select => select.JoinedFieldName))
            .ToList();
    }

    private static bool HasPropertyValue(this HydrationProperties? entityProperties, string propertyName)
    {
        return entityProperties.Exists()
               && entityProperties.ContainsKey(propertyName);
    }

    private static bool HasProperty<TQueryableEntity>(string propertyName)
        where TQueryableEntity : IQueryableEntity
    {
        var metadata = PersistedEntityMetadata.FromType<TQueryableEntity>();
        return metadata.HasType(propertyName);
    }

    private static string FirstProperty<TQueryableEntity>()
        where TQueryableEntity : IQueryableEntity
    {
        var metadata = PersistedEntityMetadata.FromType<TQueryableEntity>();
        return metadata.Types.First().Key;
    }

    private static void CopyPropertyValue(this HydrationProperties toEntityProperties,
        HydrationProperties fromEntityProperties, SelectDefinition select)
    {
        toEntityProperties.AddOrUpdate(select.JoinedFieldName, fromEntityProperties[select.FieldName]);
    }

    private static bool HasToStringMethodBeenOverridenInSubclass(Type propertyType)
    {
        var methodInfo = propertyType.GetMethod(nameof(ToString));
        if (methodInfo.NotExists())
        {
            return false;
        }

        return methodInfo.DeclaringType == propertyType;
    }
}