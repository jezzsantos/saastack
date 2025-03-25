using Application.Persistence.Interfaces;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Common;
using Common.Extensions;
using Infrastructure.External.Persistence.Azure.Extensions;
using Infrastructure.Persistence.Interfaces;
using JetBrains.Annotations;

namespace Infrastructure.External.Persistence.Azure.ApplicationServices;

/// <summary>
///     Provides a blob store for Azure Storage Account Blobs
/// </summary>
[UsedImplicitly]
public class AzureStorageAccountBlobStore : IBlobStore
{
    private readonly AzureStorageAccountStoreOptions.ConnectionOptions _connectionOptions;
    private readonly Dictionary<string, bool> _containerExistenceChecks = new();
    private readonly IRecorder _recorder;

    public static AzureStorageAccountBlobStore Create(IRecorder recorder, AzureStorageAccountStoreOptions options)
    {
        return new AzureStorageAccountBlobStore(recorder, options.Connection);
    }

    private AzureStorageAccountBlobStore(IRecorder recorder,
        AzureStorageAccountStoreOptions.ConnectionOptions connectionOptions)
    {
        _recorder = recorder;
        _connectionOptions = connectionOptions;
    }

    public async Task<Result<Error>> DeleteAsync(string containerName, string blobName,
        CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        blobName.ThrowIfNotValuedParameter(nameof(blobName),
            Resources.AnyStore_MissingBlobName);

        var container = await ConnectToContainerAsync(containerName, cancellationToken);
        try
        {
            var blob = container.GetBlobClient(blobName);
            var exists = await blob.ExistsAsync(cancellationToken);
            if (exists)
            {
                await blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, null, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to delete blob: {Blob} from: {Container}", blobName, containerName);
            return ex.ToError(ErrorCode.Unexpected);
        }

        return Result.Ok;
    }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(string containerName, CancellationToken cancellationToken)
    {
#if TESTINGONLY
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);

        var container = await ConnectToContainerAsync(containerName, cancellationToken);

        // NOTE: deleting the entire container may take far too long (this method is only tenable in testing)
        var blobs = container.GetBlobs().ToList();
        blobs.ForEach(item =>
        {
            var blob = container.GetBlobClient(item.Name);
            blob.DeleteIfExists();
        });

        _containerExistenceChecks.Remove(containerName);
#else
        await Task.CompletedTask;
#endif
        return Result.Ok;
    }
#endif

    public async Task<Result<Optional<Blob>, Error>> DownloadAsync(string containerName, string blobName, Stream stream,
        CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        blobName.ThrowIfNotValuedParameter(nameof(blobName),
            Resources.AnyStore_MissingBlobName);
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        var container = await ConnectToContainerAsync(containerName, cancellationToken);
        try
        {
            var blob = container.GetBlobClient(blobName);
            var exists = await blob.ExistsAsync(cancellationToken);
            if (!exists)
            {
                return Optional<Blob>.None;
            }

            await blob.DownloadToAsync(stream, cancellationToken);

            var properties = await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
            return new Blob
            {
                ContentType = properties.Value.ContentType
            }.ToOptional();
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to download blob: {Blob} from: {Container}", blobName, containerName);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }

    public async Task<Result<Error>> UploadAsync(string containerName, string blobName, string contentType,
        Stream stream,
        CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        blobName.ThrowIfNotValuedParameter(nameof(blobName),
            Resources.AnyStore_MissingBlobName);
        contentType.ThrowIfNotValuedParameter(nameof(contentType),
            Resources.AnyStore_MissingContentType);
        ArgumentNullException.ThrowIfNull(stream, nameof(stream));

        var container = await ConnectToContainerAsync(containerName, cancellationToken);
        try
        {
            var blob = container.GetBlobClient(blobName);

            var fromStreamAsync = await BinaryData.FromStreamAsync(stream, cancellationToken);
            await blob.UploadAsync(fromStreamAsync, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = contentType
                },
                Conditions = null // Overwrite=true
            }, cancellationToken);

            return Result.Ok;
        }
        catch (Exception ex)
        {
            _recorder.TraceError(null,
                ex, "Failed to upload blob: {Blob} to: {Container}", blobName, containerName);
            return ex.ToError(ErrorCode.Unexpected);
        }
    }

    private async Task<BlobContainerClient> ConnectToContainerAsync(string name, CancellationToken cancellationToken)
    {
        var sanitizedContainerName = name.SanitizeAndValidateStorageAccountResourceName();
        var blobClientOptions = new BlobClientOptions();
        BlobContainerClient container;
        switch (_connectionOptions.Type)
        {
            case AzureStorageAccountStoreOptions.ConnectionOptions.ConnectionType.Credentials:
                container = new BlobContainerClient(_connectionOptions.ConnectionString, sanitizedContainerName,
                    blobClientOptions);
                break;

            case AzureStorageAccountStoreOptions.ConnectionOptions.ConnectionType.ManagedIdentity:
            {
                var uri = new Uri(
                    $"https://{_connectionOptions.AccountName}.blob.core.windows.net/{sanitizedContainerName}");
                container = new BlobContainerClient(uri, _connectionOptions.Credential, blobClientOptions);
                break;
            }

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(AzureStorageAccountStoreOptions.ConnectionOptions.Type));
        }

        if (IsContainerExistenceCheckPerformed(sanitizedContainerName))
        {
            return container;
        }

        var exists = await container.ExistsAsync(cancellationToken);
        if (!exists)
        {
            await container.CreateAsync(cancellationToken: cancellationToken);
        }

        return container;
    }

    private bool IsContainerExistenceCheckPerformed(string containerName)
    {
        _containerExistenceChecks.TryAdd(containerName, false);
        if (_containerExistenceChecks[containerName])
        {
            return true;
        }

        _containerExistenceChecks[containerName] = true;

        return false;
    }
}