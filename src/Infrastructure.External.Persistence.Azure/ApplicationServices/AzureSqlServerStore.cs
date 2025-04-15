using System.Data.SqlTypes;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Domain.Interfaces.ValueObjects;
using Infrastructure.Persistence.Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Microsoft.Data.SqlClient;

namespace Infrastructure.External.Persistence.Azure.ApplicationServices;

/// <summary>
///     Provides a combined store that persists all data to a remote SQL Server (hosted either locally or in Azure).
/// </summary>
public sealed partial class AzureSqlServerStore
{
    public static readonly DateTime MaximumAllowedSqlServerDate = SqlDateTime.MaxValue.Value;
    public static readonly DateTime MinimumAllowedSqlServerDate = SqlDateTime.MinValue.Value;
    private readonly AzureSqlServerStoreOptions.ConnectionOptions _connectionOptions;
    private readonly IRecorder _recorder;

    public static AzureSqlServerStore Create(IRecorder recorder, AzureSqlServerStoreOptions options)
    {
        return new AzureSqlServerStore(recorder, options.Connection);
    }

    private AzureSqlServerStore(IRecorder recorder, AzureSqlServerStoreOptions.ConnectionOptions connectionOptions)
    {
        _recorder = recorder;
        _connectionOptions = connectionOptions;
    }

    private async Task<Result<Error>> ExecuteSqlUpdateCommandAsync(string tableName,
        Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var columnIndex = 1;
        var columnValueExpressions = string.Join(',',
            parameters.Select(p => $"{p.Key.ToColumnName()}=@{columnIndex++}"));
        var id = parameters[nameof(CommandEntity.Id)];
        var commandText =
            $"UPDATE {tableName.ToTableName()} SET {columnValueExpressions} WHERE {nameof(CommandEntity.Id)}='{id}'";

        await using var connection = new SqlConnection(_connectionOptions.ConnectionString);
        {
            try
            {
                await connection.OpenAsync(cancellationToken);
                int numRecords;
                await using (var command = new SqlCommand(commandText, connection))
                {
                    var parameterIndex = 1;
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.AddWithValue($"@{parameterIndex++}", parameter.Value);
                    }

                    numRecords = await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await connection.CloseAsync();
                _recorder.TraceInformation(null, "SQLServer executed SQL {Command}, affecting {Affecting} records",
                    commandText, numRecords);
                return Result.Ok;
            }
            catch (Exception ex)
            {
                _recorder.TraceError(null, ex, "SQLServer failed executing SQL {Command}", commandText);
                return ex.ToError(ErrorCode.Unexpected);
            }
        }
    }

    /// <summary>
    ///     Inserts the entity into the table whether it exists or not.
    /// </summary>
    private async Task<Result<Error>> ExecuteSqlInsertCommandAsync(string tableName,
        Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        var columnNames = string.Join(',', parameters.Select(p => p.Key.ToColumnName()));
        var columnIndex = 1;
        var columnValuePlaceholders = string.Join(',', parameters.Select(_ => $"@{columnIndex++}"));
        var commandText =
            $"INSERT INTO {tableName.ToTableName()} ({columnNames}) VALUES ({columnValuePlaceholders})";

        await using var connection = new SqlConnection(_connectionOptions.ConnectionString);
        {
            try
            {
                await connection.OpenAsync(cancellationToken);
                int numRecords;
                await using (var command = new SqlCommand(commandText, connection))
                {
                    var parameterIndex = 1;
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.AddWithValue($"@{parameterIndex++}", parameter.Value);
                    }

                    numRecords = await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await connection.CloseAsync();
                _recorder.TraceInformation(null, "SQLServer executed SQL {Command}, affecting {Affecting} records",
                    commandText, numRecords);
                return Result.Ok;
            }
            catch (Exception ex)
            {
                _recorder.TraceError(null, ex, "SQLServer failed executing SQL {Command}", commandText);
                return ex.ToError(ErrorCode.Unexpected);
            }
        }
    }

    /// <summary>
    ///     Inserts the entity into the table, as long as the entity does not already exist for the specified
    ///     <see cref="wheresParameter" />, otherwise return <see cref="ErrorCode.EntityExists" />
    /// </summary>
    private async Task<Result<Error>> ExecuteSqlInsertExclusiveCommandAsync(string tableName,
        Dictionary<string, object> wheresParameter,
        Dictionary<string, object> parameters, CancellationToken cancellationToken)
    {
        const int whereParameterOffset = 500; // Arbitrary offset to avoid parameter collision
        var columnNames = string.Join(',', parameters.Select(p => p.Key.ToColumnName()));
        var columnIndex = 1;
        var columnValuePlaceholders = string.Join(',', parameters.Select(_ => $"@{columnIndex++}"));
        var existsIndex = whereParameterOffset;
        var existsColumnNames = string.Join(' ', wheresParameter.Select(where =>
            $"{(existsIndex++ > whereParameterOffset ? "AND " : "")}{where.Key.ToColumnName()} = @{existsIndex - 1}"));
        var commandText = $"""
                           SET TRANSACTION ISOLATION LEVEL SERIALIZABLE 
                           BEGIN TRANSACTION 
                             IF NOT EXISTS (
                               SELECT [Id] 
                               FROM {tableName.ToTableName()} 
                               WHERE {existsColumnNames}
                               ) 
                               BEGIN 
                                 INSERT INTO {tableName.ToTableName()} ({columnNames}) 
                                 VALUES ({columnValuePlaceholders}) 
                               END 
                           COMMIT TRANSACTION
                           """;

        await using var connection = new SqlConnection(_connectionOptions.ConnectionString);
        {
            try
            {
                await connection.OpenAsync(cancellationToken);
                int numRecords;
                await using (var command = new SqlCommand(commandText, connection))
                {
                    var whereParameterIndex = whereParameterOffset;
                    foreach (var whereParameter in wheresParameter)
                    {
                        command.Parameters.AddWithValue($"@{whereParameterIndex++}", whereParameter.Value);
                    }

                    var parameterIndex = 1;
                    foreach (var parameter in parameters)
                    {
                        command.Parameters.AddWithValue($"@{parameterIndex++}", parameter.Value);
                    }

                    numRecords = await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await connection.CloseAsync();
                if (numRecords == -1)
                {
                    _recorder.TraceWarning(null, "SQLServer executed SQL {Command}, but found existing record",
                        commandText);
                    return Error.EntityExists();
                }

                _recorder.TraceInformation(null, "SQLServer executed SQL {Command}, affecting {Affecting} records",
                    commandText, numRecords);

                return Result.Ok;
            }
            catch (Exception ex)
            {
                _recorder.TraceError(null, ex, "SQLServer failed executing SQL {Command}", commandText);
                return ex.ToError(ErrorCode.Unexpected);
            }
        }
    }

    private async Task<Result<Error>> ExecuteSqlDeleteCommandAsync(string tableName,
        KeyValuePair<string, object>? whereParameter, CancellationToken cancellationToken)
    {
        var commandText = whereParameter.Exists()
            ? $"DELETE FROM {tableName.ToTableName()} WHERE {whereParameter.Value.Key.ToColumnName()}=@1"
            : $"DELETE FROM {tableName.ToTableName()}";

        await using var connection = new SqlConnection(_connectionOptions.ConnectionString);
        {
            try
            {
                await connection.OpenAsync(cancellationToken);
                int numRecords;
                await using (var command = new SqlCommand(commandText, connection))
                {
                    if (whereParameter.Exists())
                    {
                        command.Parameters.AddWithValue("@1", whereParameter.Value.Value);
                    }

                    numRecords = await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await connection.CloseAsync();
                _recorder.TraceInformation(null, "SQLServer executed SQL {Command}, affecting {Affecting} records",
                    commandText, numRecords);
                return Result.Ok;
            }
            catch (Exception ex)
            {
                _recorder.TraceError(null, ex, "SQLServer failed executing SQL {Command}", commandText);
                return ex.ToError(ErrorCode.Unexpected);
            }
        }
    }

    private async Task<Result<object, Error>> ExecuteSqlScalarCommandAsync(string commandText,
        IDictionary<string, object> parameterValuesByParameter,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionOptions.ConnectionString);
        try
        {
            object result;
            await connection.OpenAsync(cancellationToken);
            await using (var command = new SqlCommand(commandText, connection))
            {
                foreach (var pair in parameterValuesByParameter)
                {
                    command.Parameters.AddWithValue(pair.Key, pair.Value);
                }

                result = await command.ExecuteScalarAsync(cancellationToken);
            }

            await connection.CloseAsync();
            _recorder.TraceInformation(null, "SQLServer executed SQL {Command}, with result {Result}", commandText,
                result);

            return result;
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, ex, "SQLServer failed executing SQL {Command}", commandText);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }

    private async Task<Result<Dictionary<string, object>, Error>> ExecuteSqlSelectSingleCommandAsync(string tableName,
        KeyValuePair<string, object>? whereParameter, CancellationToken cancellationToken)
    {
        var commandText = whereParameter.Exists()
            ? $"SELECT * FROM {tableName.ToTableName()} WHERE {whereParameter.Value.Key.ToColumnName()}=@1"
            : $"SELECT * FROM {tableName.ToTableName()}";

        await using var connection = new SqlConnection(_connectionOptions.ConnectionString);
        {
            try
            {
                await connection.OpenAsync(cancellationToken);
                Dictionary<string, object> result;
                int numRecords;
                await using (var command = new SqlCommand(commandText, connection))
                {
                    if (whereParameter.Exists())
                    {
                        command.Parameters.AddWithValue("@1", whereParameter.Value.Value);
                    }

                    await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        var hasRead = await reader.ReadAsync(cancellationToken);
                        if (!hasRead)
                        {
                            return new Dictionary<string, object>();
                        }

                        numRecords = 1;
                        result = Enumerable.Range(0, reader.FieldCount)
                            .ToDictionary(
                                columnIndex => reader.GetName(columnIndex),
                                columnIndex => reader.GetValue(columnIndex));
                    }
                }

                await connection.CloseAsync();
                _recorder.TraceInformation(null, "SQLServer executed SQL {Command}, returning {Affecting} records",
                    commandText, numRecords);
                return result;
            }
            catch (Exception ex)
            {
                _recorder.TraceError(null, ex, "SQLServer failed executing SQL {Command}", tableName);
                return ex.ToError(ErrorCode.Unexpected);
            }
        }
    }

    private async Task<Result<List<Dictionary<string, object>>, Error>> ExecuteSqlSelectCommandAsync(
        string commandText, IDictionary<string, object> parameterValuesByParameter, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_connectionOptions.ConnectionString);
        {
            try
            {
                await connection.OpenAsync(cancellationToken);
                var results = new List<Dictionary<string, object>>();
                await using (var command = new SqlCommand(commandText, connection))
                {
                    foreach (var pair in parameterValuesByParameter)
                    {
                        command.Parameters.AddWithValue(pair.Key, pair.Value);
                    }

                    await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            var result = ReaderToObjectDictionary(reader);
                            OverwriteJoinedSelects(result);
                            results.Add(result);
                        }
                    }
                }

                var numRecords = results.Count;
                _recorder.TraceInformation(null, "SQLServer executed SQL {Command}, affecting {Affecting} records",
                    commandText, numRecords);
                return results;
            }
            catch (Exception ex)
            {
                _recorder.TraceError(null, ex, "SQLServer failed executing SQL {Command}", commandText);
                return ex.ToError(ErrorCode.Unexpected);
            }
        }

        static void OverwriteJoinedSelects(IDictionary<string, object> result)
        {
            var overwrites = new Dictionary<string, object>();
            foreach (var (key, value) in result)
            {
                if (key.StartsWith(JoinedEntityFieldAliasPrefix))
                {
                    var primaryFieldName = key.Remove(0, JoinedEntityFieldAliasPrefix.Length);
                    if (!value.IsDbNull())
                    {
                        overwrites.Add(primaryFieldName, value);
                    }
                }
            }

            if (overwrites.Any())
            {
                foreach (var (key, value) in overwrites)
                {
                    result[key] = value;
                }
            }
        }

        static Dictionary<string, object> ReaderToObjectDictionary(SqlDataReader sqlDataReader)
        {
            var result = new Dictionary<string, object>();
            Enumerable.Range(0, sqlDataReader.FieldCount)
                .ToList()
                .ForEach(column =>
                {
                    var name = sqlDataReader.GetName(column);
                    result.TryAdd(name, sqlDataReader.GetValue(column));
                });

            return result;
        }
    }
}

internal static class AzureSqlServerStoreConversionExtensions
{
    public static HydrationProperties FromTableEntity(this Dictionary<string, object> tableProperties,
        PersistedEntityMetadata metadata)

    {
        var properties = tableProperties
            .Where(pair => metadata.HasType(pair.Key))
            .ToDictionary(pair => pair.Key,
                pair => pair.FromTableEntityProperty(metadata.GetPropertyType(pair.Key)));

        ApplyMappings(metadata, tableProperties, properties);

        if (properties.ContainsKey(nameof(CommandEntity.Id)))
        {
            return new HydrationProperties(properties);
        }

        var id = tableProperties[nameof(CommandEntity.Id)].ToString()!;
        properties[nameof(CommandEntity.Id)] = id;

        return new HydrationProperties(properties);
    }

    public static bool IsDbNull(this object value)
    {
        var isNullBinary = value is SqlBinary { IsNull: true };
        return value.NotExists()
               || value == DBNull.Value
               || isNullBinary;
    }

    public static bool IsMaximumAllowedSqlServerDate(this DateTime dateTime)
    {
        return dateTime >= AzureSqlServerStore.MaximumAllowedSqlServerDate;
    }

    public static string ToColumnName(this string columnName)
    {
        return $"[{columnName}]";
    }

    public static Dictionary<string, object> ToTableEntity(this CommandEntity entity)
    {
        var properties = new Dictionary<string, object>();
        foreach (var (key, value) in entity.Properties)
        {
            var targetPropertyType = entity.GetPropertyType(key);
            properties.Add(key, ToTableEntityProperty(value, targetPropertyType));
        }

        properties[nameof(CommandEntity.LastPersistedAtUtc)] = DateTime.UtcNow;

        return properties;
    }

    public static string ToTableName(this string tableName)
    {
        return $"[{tableName}]";
    }

    private static object ToTableEntityProperty(Optional<object> propertyValue, Optional<Type> targetPropertyType)
    {
        if (targetPropertyType.HasValue &&
            (targetPropertyType.Value.IsEnum
             || targetPropertyType.Value.IsNullableEnum()
             || targetPropertyType.Value.IsOptionalEnum()))
        {
            return ToValue(propertyValue);
        }

        if (propertyValue.HasValue
            && propertyValue.Value.GetType().IsComplexStorageType())
        {
            var thing = propertyValue.ComplexTypeToContainerProperty();
            return ToValue(thing);
        }

        if (!propertyValue.HasValue)
        {
            return ToValue(propertyValue);
        }

        switch (propertyValue.Value)
        {
            case DateTime dateTime:
                return ToDate(dateTime);

            case DateTimeOffset dateTimeOffset:
                if (dateTimeOffset == DateTimeOffset.MinValue)
                {
                    dateTimeOffset = DateTimeOffset.MinValue.ToUniversalTime();
                }

                return dateTimeOffset.ToIso8601();

            case Guid guid:
                return guid.ToString("D");

            case byte[] bytes:
                return bytes;

            case null:
                return ToValue(propertyValue);

            default:
                return propertyValue.Value;
        }

        object ToValue(Optional<object> value)
        {
            if (!value.HasValue)
            {
                if (targetPropertyType == typeof(byte[]))
                {
                    return SqlBinary.Null;
                }

                return DBNull.Value;
            }

            if (value.Value.NotExists())
            {
                return DBNull.Value;
            }

            var stringValue = value.Value.ToString();
            if (stringValue.NotExists())
            {
                return DBNull.Value;
            }

            return stringValue;
        }

        object ToDate(DateTime dateTime)
        {
            if (!dateTime.HasValue())
            {
                return DBNull.Value;
            }

            return dateTime.IsNotAllowedBySqlServerDate()
                ? AzureSqlServerStore.MinimumAllowedSqlServerDate
                : dateTime.ToUniversalTime();
        }
    }

    private static bool IsNotAllowedBySqlServerDate(this DateTime dateTime)
    {
        if (dateTime.HasValue())
        {
            return dateTime.ToUniversalTime() <= AzureSqlServerStore.MinimumAllowedSqlServerDate;
        }

        return true;
    }

    private static bool IsMinimumSqlServerDate(this DateTime dateTime)
    {
        return dateTime <= AzureSqlServerStore.MinimumAllowedSqlServerDate;
    }

    private static Optional<object> FromTableEntityProperty(this KeyValuePair<string, object> property,
        Type targetPropertyType)
    {
        var propertyValue = property.Value;

        if (propertyValue == DBNull.Value)
        {
            if (targetPropertyType == typeof(DateTime))
            {
                return DateTime.MinValue.ToUniversalTime();
            }

            return Optional<object>.None;
        }

        if (targetPropertyType == typeof(string)
            || targetPropertyType == typeof(Optional<string>)
            || targetPropertyType == typeof(Optional<string?>))
        {
            return propertyValue;
        }

        if (targetPropertyType == typeof(bool) || targetPropertyType == typeof(bool?)
                                               || targetPropertyType == typeof(Optional<bool>)
                                               || targetPropertyType == typeof(Optional<bool?>))
        {
            if (propertyValue is bool boolValue)
            {
                return boolValue;
            }

            return Optional<object>.None;
        }

        if (targetPropertyType == typeof(DateTime) || targetPropertyType == typeof(DateTime?)
                                                   || targetPropertyType == typeof(Optional<DateTime>)
                                                   || targetPropertyType == typeof(Optional<DateTime?>))
        {
            if (propertyValue is DateTime dateValue)
            {
                return dateValue.IsMinimumSqlServerDate()
                    ? DateTime.MinValue.ToUniversalTime()
                    : DateTime.SpecifyKind(dateValue, DateTimeKind.Utc);
            }

            return Optional<object>.None;
        }

        if (targetPropertyType == typeof(DateTimeOffset) || targetPropertyType == typeof(DateTimeOffset?)
                                                         || targetPropertyType == typeof(Optional<DateTimeOffset>)
                                                         || targetPropertyType == typeof(Optional<DateTimeOffset?>))
        {
            if (propertyValue is DateTimeOffset dateValue)
            {
                return dateValue;
            }

            return DateTime.MinValue;
        }

        if (targetPropertyType == typeof(Guid) || targetPropertyType == typeof(Guid?)
                                               || targetPropertyType == typeof(Optional<Guid>)
                                               || targetPropertyType == typeof(Optional<Guid?>))
        {
            if (propertyValue is string stringValue)
            {
                return Guid.Parse(stringValue);
            }

            return Optional<object>.None;
        }

        if (targetPropertyType == typeof(decimal) || targetPropertyType == typeof(decimal?)
                                                  || targetPropertyType == typeof(Optional<decimal>)
                                                  || targetPropertyType == typeof(Optional<decimal?>))
        {
            if (propertyValue is decimal numberValue)
            {
                return numberValue;
            }

            return Optional<object>.None;
        }

        if (targetPropertyType == typeof(double) || targetPropertyType == typeof(double?)
                                                 || targetPropertyType == typeof(Optional<double>)
                                                 || targetPropertyType == typeof(Optional<double?>))
        {
            if (propertyValue is double numberValue)
            {
                return numberValue;
            }

            return Optional<object>.None;
        }

        if (targetPropertyType == typeof(long) || targetPropertyType == typeof(long?)
                                               || targetPropertyType == typeof(Optional<long>)
                                               || targetPropertyType == typeof(Optional<long?>))
        {
            if (propertyValue is long numberValue)
            {
                return numberValue;
            }

            return Optional<object>.None;
        }

        if (targetPropertyType == typeof(int) || targetPropertyType == typeof(int?)
                                              || targetPropertyType == typeof(Optional<int>)
                                              || targetPropertyType == typeof(Optional<int?>))
        {
            if (propertyValue is int smallNumberValue)
            {
                return smallNumberValue;
            }

            if (propertyValue is long largeNumberValue)
            {
                return (int)largeNumberValue;
            }

            return Optional<object>.None;
        }

        if (targetPropertyType == typeof(byte[]) || targetPropertyType == typeof(Optional<byte[]>))
        {
            if (propertyValue is byte[] arrayValue)
            {
                return arrayValue;
            }

            return Optional<object>.None;
        }

        if (targetPropertyType.IsEnum)
        {
            if (propertyValue is string stringValue)
            {
                return Enum.Parse(targetPropertyType, stringValue, true);
            }

            return Optional<object>.None;
        }

        if (targetPropertyType.IsOptionalEnum())
        {
            if (propertyValue is string stringValue)
            {
                var underlyingType = targetPropertyType.GetGenericArguments().First();
                return Enum.Parse(underlyingType, stringValue, true);
            }

            return Optional<object>.None;
        }

        if (targetPropertyType.IsNullableEnum())
        {
            if (propertyValue is string stringValue)
            {
                if (stringValue.HasValue())
                {
                    return targetPropertyType.ParseNullable(stringValue);
                }

                return Optional<object>.None;
            }

            return Optional<object>.None;
        }

        if (targetPropertyType.IsComplexStorageType())
        {
            if (propertyValue is string stringValue)
            {
                return stringValue.ToOptional()
                    .ComplexTypeFromContainerProperty(targetPropertyType);
            }

            return Optional<object>.None;
        }

        if (typeof(IDehydratableValueObject).IsAssignableFrom(targetPropertyType))
        {
            return propertyValue;
        }

        return propertyValue;
    }

    private static void ApplyMappings(PersistedEntityMetadata metadata,
        Dictionary<string, object> containerProperties,
        Dictionary<string, Optional<object>>? properties)
    {
        if (properties.NotExists())
        {
            return;
        }

        var mappings = metadata.GetReadMappingsOverride();
        if (mappings.HasAny())
        {
            var containerPropertiesDictionary = containerProperties
                .ToDictionary<KeyValuePair<string, object>, string, object?>(pair => pair.Key, pair => pair.Value);
            foreach (var mapping in mappings)
            {
                var mapResult = Try.Safely(() => mapping.Value(containerPropertiesDictionary));
                if (mapResult.Exists())
                {
                    if (!properties.TryAdd(mapping.Key, mapResult))
                    {
                        properties[mapping.Key] = mapResult;
                    }
                }
            }
        }
    }
}