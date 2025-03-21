using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Infrastructure.Persistence.OnPremises.Extensions;
using Microsoft.Data.SqlClient;

namespace Infrastructure.Persistence.OnPremises.ApplicationServices;

public sealed partial class SqlServerStore : IBlobStore
{
    private readonly Dictionary<string, bool> _blobContainerExistenceChecks = new();
    private static string? _cachedBlobContainerName;

    public async Task<Result<Error>> DeleteAsync(string containerName, string blobName,
        CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName), Resources.AnyStore_MissingContainerName);
        blobName.ThrowIfNotValuedParameter(nameof(blobName), Resources.AnyStore_MissingBlobName);

        var tableName = await ConnectToBlobContainerAsync(containerName, cancellationToken);
        return await ExecuteSqlDeleteCommandAsync(tableName, new KeyValuePair<string, object>("BlobName", blobName),
            cancellationToken);
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(string containerName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName), Resources.AnyStore_MissingContainerName);
        var tableName = await ConnectToBlobContainerAsync(containerName, cancellationToken);
        return await ExecuteSqlDeleteCommandAsync(tableName, null, cancellationToken);
    }
#endif

    public async Task<Result<Optional<Blob>, Error>> DownloadAsync(string containerName, string blobName, Stream stream,
        CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName), Resources.AnyStore_MissingContainerName);
        blobName.ThrowIfNotValuedParameter(nameof(blobName), Resources.AnyStore_MissingBlobName);
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        var tableName = await ConnectToBlobContainerAsync(containerName, cancellationToken);
        var queryResult = await ExecuteSqlSelectSingleCommandAsync(tableName,
            new KeyValuePair<string, object>("BlobName", blobName), cancellationToken);
        if (queryResult.IsFailure)
        {
            return queryResult.Error;
        }

        var record = queryResult.Value;
        if (!record.Any())
        {
            return Optional<Blob>.None;
        }

        var contentType = record["ContentType"] as string;
        var data = record["Data"] as byte[] ?? Array.Empty<byte>();
        await stream.WriteAsync(data, 0, data.Length, cancellationToken);
        await stream.FlushAsync(cancellationToken);
        return new Blob { ContentType = contentType }.ToOptional();
    }

    public async Task<Result<Error>> UploadAsync(string containerName, string blobName, string contentType,
        Stream stream, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName), Resources.AnyStore_MissingContainerName);
        blobName.ThrowIfNotValuedParameter(nameof(blobName), Resources.AnyStore_MissingBlobName);
        contentType.ThrowIfNotValuedParameter(nameof(contentType), Resources.AnyStore_MissingContentType);
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        var tableName = await ConnectToBlobContainerAsync(containerName, cancellationToken);

        byte[] data;
        using (var memory = new MemoryStream())
        {
            await stream.CopyToAsync(memory, cancellationToken);
            data = memory.ToArray();
        }

        var deleteResult = await ExecuteSqlDeleteCommandAsync(tableName,
            new KeyValuePair<string, object>("BlobName", blobName), cancellationToken);
        if (deleteResult.IsFailure)
        {
            return deleteResult.Error;
        }

        var newIdBlobStorage = $"bs_{Guid.NewGuid():N}";
        var insertParameters = new Dictionary<string, object>
        {
            { "Id", newIdBlobStorage },
            { "BlobName", blobName },
            { "ContentType", contentType },
            { "Data", data }
        };

        return await ExecuteSqlInsertCommandAsync(tableName, insertParameters, cancellationToken);
    }

    private async Task<string> ConnectToBlobContainerAsync(string containerName, CancellationToken cancellationToken)
    {
        var sanitizedContainerName = containerName.SanitizeAndValidateInvalidDatabaseResourceName();

        if (_cachedBlobContainerName != null &&
            string.Equals(_cachedBlobContainerName, sanitizedContainerName,
                StringComparison.InvariantCultureIgnoreCase))
        {
            return _cachedBlobContainerName;
        }

        if (IsBlobContainerExistenceCheckPerformed(sanitizedContainerName))
        {
            _cachedBlobContainerName = sanitizedContainerName;
            return sanitizedContainerName;
        }

        using var conn = new SqlConnection(_connectionOptions.ConnectionString);
        await conn.OpenAsync(cancellationToken);
        try
        {
            var checkCmd = conn.CreateCommand();
            checkCmd.CommandText = "SELECT 1 FROM sys.tables WHERE [name] = @TableName";
            checkCmd.Parameters.AddWithValue("@TableName", sanitizedContainerName);
            var existsObj = await checkCmd.ExecuteScalarAsync(cancellationToken);
            if (existsObj == null)
            {
                var createSql = $@"
                        CREATE TABLE [dbo].[{sanitizedContainerName}] (
                            [Id]          nvarchar(100) NOT NULL,
                            [BlobName]    NVARCHAR(256) NOT NULL,
                            [ContentType] NVARCHAR(256) NULL,
                            [Data]        VARBINARY(MAX) NULL,
                            CONSTRAINT [PK_{sanitizedContainerName}] PRIMARY KEY CLUSTERED ([Id],[BlobName] ASC)
                        )";
                var createCmd = conn.CreateCommand();
                createCmd.CommandText = createSql;
                await createCmd.ExecuteNonQueryAsync(cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null, ex, "Failed to create or find table for container: {Container}",
                sanitizedContainerName);
            throw;
        }

        _blobContainerExistenceChecks.TryAdd(sanitizedContainerName, true);
        _cachedBlobContainerName = sanitizedContainerName;
        return sanitizedContainerName;
    }

    private bool IsBlobContainerExistenceCheckPerformed(string containerName)
    {
        _blobContainerExistenceChecks.TryAdd(containerName, false);
        if (_blobContainerExistenceChecks[containerName])
        {
            return true;
        }

        _blobContainerExistenceChecks[containerName] = true;
        return false;
    }
}