using System.Text;
using Common;
using Common.Extensions;
using Domain.Interfaces.ValueObjects;
using Infrastructure.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using QueryAny;

namespace Infrastructure.Persistence.Azure.ApplicationServices;

partial class AzureSqlServerStore : IDataStore
{
    internal const string JoinedEntityFieldAliasPrefix = @"je_";
    internal const string PrimaryTableAlias = @"t";

    public async Task<Result<CommandEntity, Error>> AddAsync(string containerName, CommandEntity entity,
        CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        ArgumentNullException.ThrowIfNull(entity);

        var executed = await ExecuteSqlInsertCommandAsync(containerName, entity.ToTableEntity(), cancellationToken);
        if (executed.IsFailure)
        {
            return executed.Error;
        }

        var retrieved = await RetrieveAsync(containerName, entity.Id, entity.Metadata, cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        return retrieved.Value.Value;
    }

    public async Task<Result<long, Error>> CountAsync(string containerName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);

        var executed = await ExecuteSqlScalarCommandAsync(
            $"SELECT COUNT({nameof(CommandEntity.Id).ToColumnName()}) FROM {containerName.ToTableName()}",
            cancellationToken);
        if (executed.IsFailure)
        {
            return executed.Error;
        }

        var count = executed.Value;
        return Convert.ToInt64(count);
    }

#if TESTINGONLY
    async Task<Result<Error>> IDataStore.DestroyAllAsync(string containerName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);

        return await ExecuteSqlDeleteCommandAsync(containerName, null, cancellationToken);
    }
#endif
    public int MaxQueryResults => 1000;

    public async Task<Result<List<QueryEntity>, Error>> QueryAsync<TQueryableEntity>(string containerName,
        QueryClause<TQueryableEntity> query, PersistedEntityMetadata metadata, CancellationToken cancellationToken)
        where TQueryableEntity : IQueryableEntity
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(metadata);

        if (query.NotExists() || query.Options.IsEmpty)
        {
            return new List<QueryEntity>();
        }

        var take = query.GetDefaultTake(MaxQueryResults);
        if (take == 0)
        {
            return new List<QueryEntity>();
        }

        var (select, queryParameters) = query.ToSqlServerQuery(containerName, this);

        var executed = await ExecuteSqlSelectCommandAsync(select, queryParameters, cancellationToken);
        if (executed.IsFailure)
        {
            return executed.Error;
        }

        var results = executed.Value;
        return results
            .Select(properties => QueryEntity.FromProperties(properties.FromTableEntity(metadata), metadata))
            .ToList();
    }

    public async Task<Result<Error>> RemoveAsync(string containerName, string id, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        id.ThrowIfNotValuedParameter(nameof(id), Resources.AnyStore_MissingId);

        return await ExecuteSqlDeleteCommandAsync(containerName,
            new KeyValuePair<string, object>(nameof(CommandEntity.Id), id), cancellationToken);
    }

    public async Task<Result<Optional<CommandEntity>, Error>> ReplaceAsync(string containerName, string id,
        CommandEntity entity, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        id.ThrowIfNotValuedParameter(nameof(id), Resources.AnyStore_MissingId);
        ArgumentNullException.ThrowIfNull(entity);

        var updatedEntity = entity.ToTableEntity();

        var executed = await ExecuteSqlUpdateCommandAsync(containerName, updatedEntity, cancellationToken);
        if (executed.IsFailure)
        {
            return executed.Error;
        }

        return CommandEntity.FromCommandEntity(updatedEntity.FromTableEntity(entity.Metadata), entity)
            .ToOptional();
    }

    public async Task<Result<Optional<CommandEntity>, Error>> RetrieveAsync(string containerName, string id,
        PersistedEntityMetadata metadata, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName), Resources.AnyStore_MissingContainerName);
        id.ThrowIfNotValuedParameter(nameof(id), Resources.AnyStore_MissingId);
        ArgumentNullException.ThrowIfNull(metadata);

        var executed = await ExecuteSqlSelectSingleCommandAsync(containerName,
            new KeyValuePair<string, object>(nameof(CommandEntity.Id), id), cancellationToken);
        if (executed.IsFailure)
        {
            return executed.Error;
        }

        var properties = executed.Value;
        if (properties.Count > 0)
        {
            return CommandEntity.FromCommandEntity(properties.FromTableEntity(metadata), metadata)
                .ToOptional();
        }

        return Optional<CommandEntity>.None;
    }
}

internal static class SqlServerQueryBuilderExtensions
{
    public static (string Query, Dictionary<string, object> Parameters) ToSqlServerQuery<TQueryableEntity>(
        this QueryClause<TQueryableEntity> query,
        string tableName, IDataStore store)
        where TQueryableEntity : IQueryableEntity
    {
        var builder = new StringBuilder();
        builder.Append($"SELECT {query.ToSelectClause()}");
        builder.Append($" FROM {tableName.ToAliasedTableName()}");

        var joins = query.JoinedEntities.ToJoinClause();
        if (joins.HasValue)
        {
            builder.Append($"{joins}");
        }

        var (wheres, queryParameters) = query.Wheres.ToWhereClause(query.JoinedEntities);
        if (wheres.HasValue)
        {
            builder.Append($" WHERE {wheres}");
        }

        var orderBy = query.ToOrderByClause(query.JoinedEntities);
        if (orderBy.HasValue())
        {
            builder.Append($" ORDER BY {orderBy}");
        }

        var skip = query.GetDefaultSkip();
        var take = query.GetDefaultTake(store.MaxQueryResults);
        builder.Append(take > 0
            ? $" OFFSET {skip} ROWS FETCH NEXT {take} ROWS ONLY"
            : $" OFFSET {skip} ROWS");

        return (builder.ToString(), queryParameters);
    }

    /// <summary>
    ///     Note: A SelectFromJoin cannot exist without a corresponding Join defined.
    ///     SelectedPrimaries? | SelectFromJoin? | Joins? | Result
    ///     -------------------|-----------------|--------|------------------------------------------------
    ///     False                     False        False    *
    ///     False                     True         True     AppendSelectedJoinedAliased
    ///     True                      False        False    AppendSelectedPrimaries
    ///     True                      True         True     AppendSelectedPrimaries + AppendSelectedJoinedAliased
    ///     False                     False        True     AppendAllPrimaries
    ///     True                      False        True     AppendSelectedPrimaries
    /// </summary>
    private static string ToSelectClause<TQueryableEntity>(this QueryClause<TQueryableEntity> query)
        where TQueryableEntity : IQueryableEntity
    {
        var primaryEntity = query.PrimaryEntity;
        var joinedEntities = query.JoinedEntities;
        if (primaryEntity.Selects.HasAny())
        {
            if (joinedEntities.HasNone())
            {
                return AppendSelectedPrimaries();
            }

            return joinedEntities.SelectMany(qe => qe.Selects).HasAny()
                ? AppendSelectedPrimariesPlusSelectedJoinedAliased()
                : AppendSelectedPrimaries();
        }

        if (joinedEntities.HasAny())
        {
            return joinedEntities.SelectMany(qe => qe.Selects).HasAny()
                ? AppendSelectedJoinedAliased(true)
                : AppendAllPrimaries();
        }

        return @"*";

        static string AppendAllPrimaries()
        {
            return $"{AzureSqlServerStore.PrimaryTableAlias}.*";
        }

        string AppendSelectedPrimaries()
        {
            var builder = new StringBuilder();
            builder.Append($"{ToAliasedColumnName(nameof(QueryEntity.Id))}");
            var nonIdSelects = primaryEntity.Selects
                .Where(def => def.FieldName.NotEqualsOrdinal(nameof(QueryEntity.Id)));
            foreach (var select in nonIdSelects)
            {
                builder.Append($", {ToAliasedColumnName(select.FieldName)}");
            }

            return builder.ToString();
        }

        string AppendSelectedJoinedAliased(bool includePrimary = false)
        {
            var builder = new StringBuilder();
            builder.Append(ToAliasedColumnName(nameof(QueryEntity.Id)));

            foreach (var join in joinedEntities.SelectMany(qe => qe.Selects))
            {
                if (includePrimary)
                {
                    builder.Append($", {ToAliasedColumnName(join.FieldName)}");
                }

                builder.Append(
                    $", {DetermineTableColumnName(join.EntityName, join.FieldName)} AS {ToJoinedColumnName(join.JoinedFieldName)}");
            }

            return builder.ToString();
        }

        string AppendSelectedPrimariesPlusSelectedJoinedAliased()
        {
            var builder = new StringBuilder();

            builder.Append(AppendSelectedPrimaries());
            builder.Append(", ");
            builder.Append(AppendSelectedJoinedAliased());

            return builder.ToString();
        }
    }

    private static string ToOrderByClause<TQueryableEntity>(this QueryClause<TQueryableEntity> query,
        IReadOnlyList<QueriedEntity> joinedEntities)
        where TQueryableEntity : IQueryableEntity
    {
        var orderBy = query.ResultOptions.OrderBy;
        var direction = orderBy.Direction == OrderDirection.Ascending
            ? @"ASC"
            : @"DESC";
        var by = query.GetDefaultOrdering();

        var columnName = ToOrderByColumnName(by, joinedEntities);
        return $"{columnName} {direction}";
    }

    private static string ToOrderByColumnName(string by, IReadOnlyList<QueriedEntity> joinedEntities)
    {
        if (joinedEntities.HasAny())
        {
            var joinedField = joinedEntities
                .SelectMany(qe => qe.Selects)
                .FirstOrDefault(def => def.JoinedFieldName.EqualsIgnoreCase(by));
            if (joinedField.Exists())
            {
                var joinLeftFieldName = ToJoinedColumnName(joinedField.JoinedFieldName);
                return joinLeftFieldName;
            }
        }

        return ToAliasedColumnName(by);
    }

    private static Optional<string> ToJoinClause(this IReadOnlyList<QueriedEntity> joinedEntities)
    {
        if (joinedEntities.HasNone())
        {
            return Optional<string>.None;
        }

        var builder = new StringBuilder();
        foreach (var entity in joinedEntities)
        {
            builder.Append(entity.Join.ToJoinClause());
        }

        return builder.ToString();
    }

    private static string ToJoinClause(this JoinDefinition join)
    {
        var joinType = join.Type.ToJoinClauseType();

        return
            $" {joinType} JOIN {join.Right.EntityName} ON {ToAliasedColumnName(join.Left.JoinedFieldName)}={DetermineTableColumnName(join.Right.EntityName, join.Right.JoinedFieldName)}";
    }

    private static string ToJoinClauseType(this JoinType type)
    {
        return type switch
        {
            JoinType.Left => "LEFT",
            JoinType.Inner => "INNER",
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static (Optional<string> Clause, Dictionary<string, object> Parameters) ToWhereClause(
        this IReadOnlyList<WhereExpression> wheres,
        IReadOnlyList<QueriedEntity> joinedEntities)
    {
        if (wheres.HasNone())
        {
            return (Optional<string>.None, new Dictionary<string, object>());
        }

        var queryParameters = new Dictionary<string, object>();
        var builder = new StringBuilder();
        foreach (var where in wheres)
        {
            builder.Append(where.ToWhereClause(joinedEntities, ref queryParameters));
        }

        return (builder.ToString(), queryParameters);
    }

    private static string ToWhereClause(this WhereExpression where,
        IReadOnlyList<QueriedEntity> joinedEntities, ref Dictionary<string, object> queryParameters)
    {
        if (where.Condition != null)
        {
            var condition = where.Condition;

            return
                $"{where.Operator.ToConditionLogicalOperator()}{condition.ToConditionClause(joinedEntities, ref queryParameters)}";
        }

        if (where.NestedWheres != null && where.NestedWheres.HasAny())
        {
            var builder = new StringBuilder();
            builder.Append($"{where.Operator.ToConditionLogicalOperator()}");
            builder.Append('(');
            foreach (var nestedWhere in where.NestedWheres)
            {
                builder.Append($"{nestedWhere.ToWhereClause(joinedEntities, ref queryParameters)}");
            }

            builder.Append(')');

            return builder.ToString();
        }

        return string.Empty;
    }

    private static string ToConditionClause(this WhereCondition condition,
        IReadOnlyList<QueriedEntity> joinedEntities, ref Dictionary<string, object> queryParameters)
    {
        var columnName = ToColumnName(condition.FieldName, joinedEntities);
        var @operator = condition.Operator.ToConditionOperator();

        var value = condition.Value;
        switch (value)
        {
            case string text:
                var parameterIndex = queryParameters.Count;
                queryParameters["p" + parameterIndex] = text;

                if (condition.Operator == ConditionOperator.Like)
                {
                    return $"CHARINDEX(@p{parameterIndex}, {columnName}) > 0";
                }

                return $"{columnName} {@operator} @p{parameterIndex}";

            case DateTime dateTime:
                if (dateTime.HasValue())
                {
                    return dateTime.IsMaximumAllowedSqlServerDate()
                        ? $"{columnName} {@operator} '{AzureSqlServerStore.MaximumAllowedSqlServerDate:yyyy-MM-dd HH:mm:ss.fff}'"
                        : $"{columnName} {@operator} '{dateTime:yyyy-MM-dd HH:mm:ss.fff}'";
                }

                return
                    $"({columnName} {@operator} '{AzureSqlServerStore.MinimumAllowedSqlServerDate:yyyy-MM-dd HH:mm:ss.fff}'"
                    + $" OR {columnName} {(condition.Operator == ConditionOperator.EqualTo ? "IS" : @operator)} NULL)";

            case DateTimeOffset dateTimeOffset:
                return
                    $"{columnName} {@operator} '{dateTimeOffset:O}'";

            case bool boolean:
                return
                    $"{columnName} {@operator} {(boolean ? 1 : 0)}";

            case double _:
            case decimal _:
            case int _:
            case long _:
                return $"{columnName} {@operator} {value}";

            case byte[] bytes:
                return
                    $"{columnName} {@operator} {ToHexBytes(bytes)}";

            case Guid guid:
                return $"{columnName} {@operator} '{guid:D}'";

            case null:
                return condition.Operator == ConditionOperator.EqualTo
                    ? $"{columnName} IS NULL"
                    : $"{columnName} IS NOT NULL";

            default:
                return value.ToWhereCondition(columnName, @operator, ref queryParameters);
        }
    }

    private static string ToWhereCondition(this object value, string columnName, string @operator,
        ref Dictionary<string, object> queryParameters)
    {
        if (value.NotExists())
        {
            return $"{columnName} {@operator} NULL";
        }

        var parameterIndex = queryParameters.Count;

        if (value is IDehydratableValueObject valueObject)
        {
            queryParameters["p" + parameterIndex] = valueObject.Dehydrate();
        }
        else
        {
            queryParameters["p" + parameterIndex] = value.ToString()!;
        }

        return $"{columnName} {@operator} @p{parameterIndex}";
    }

    private static string ToConditionLogicalOperator(this LogicalOperator op)
    {
        return op switch
        {
            LogicalOperator.And => " AND ",
            LogicalOperator.Or => " OR ",
            LogicalOperator.None => string.Empty,
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }

    private static string ToConditionOperator(this ConditionOperator op)
    {
        return op switch
        {
            ConditionOperator.EqualTo or ConditionOperator.Like => "=",
            ConditionOperator.GreaterThan => ">",
            ConditionOperator.GreaterThanEqualTo => ">=",
            ConditionOperator.LessThan => "<",
            ConditionOperator.LessThanEqualTo => "<=",
            ConditionOperator.NotEqualTo => "<>",
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null)
        };
    }

    private static string ToColumnName(string columnName, IReadOnlyList<QueriedEntity> joinedEntities)
    {
        if (joinedEntities.HasAny())
        {
            var joinedField = joinedEntities
                .SelectMany(qe => qe.Selects)
                .FirstOrDefault(sel => sel.JoinedFieldName.EqualsIgnoreCase(columnName));
            if (joinedField.Exists())
            {
                return DetermineTableColumnName(joinedField.EntityName, joinedField.FieldName);
            }
        }

        return ToAliasedColumnName(columnName);
    }

    private static string ToJoinedColumnName(string columnName)
    {
        return $"{AzureSqlServerStore.JoinedEntityFieldAliasPrefix}{columnName}".ToColumnName();
    }

    private static string ToHexBytes(byte[] bytes)
    {
        if (bytes.NotExists() || bytes.Length == 0)
        {
            return @"NULL";
        }

        var sequence = BitConverter.ToString(bytes).Replace("-", "");
        return $"0x{sequence}";
    }

    private static string ToAliasedTableName(this string tableName)
    {
        return $"[{tableName}] {AzureSqlServerStore.PrimaryTableAlias}";
    }

    private static string ToAliasedColumnName(this string columnName)
    {
        return $"{AzureSqlServerStore.PrimaryTableAlias}.{columnName.ToColumnName()}";
    }

    private static string DetermineTableColumnName(string tableName, string columnName)
    {
        return $"{tableName.ToTableName()}.{columnName.ToColumnName()}";
    }
}