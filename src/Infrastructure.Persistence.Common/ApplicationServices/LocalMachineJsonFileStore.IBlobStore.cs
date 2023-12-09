#if TESTINGONLY
using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Persistence.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.Common.ApplicationServices;

public partial class LocalMachineJsonFileStore : IBlobStore
{
    private const string BlobStoreContainerName = "Blobs";

    Task<Result<Error>> IBlobStore.DestroyAllAsync(string containerName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);

        var blobStore = EnsureContainer(GetBlobStoreContainerPath(containerName, null));
        blobStore.Erase();

        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Error>> DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        blobName.ThrowIfNotValuedParameter(nameof(blobName), Resources.InProcessInMemDataStore_MissingBlobName);

        var container = EnsureContainer(GetBlobStoreContainerPath(containerName, null));

        container.Remove(blobName);

        return Task.FromResult(Result.Ok);
    }

    public Task<Result<Optional<Blob>, Error>> DownloadAsync(string containerName, string blobName, Stream stream,
        CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        blobName.ThrowIfNotValuedParameter(nameof(blobName), Resources.InProcessInMemDataStore_MissingBlobName);
        ArgumentNullException.ThrowIfNull(stream);

        var container = EnsureContainer(GetBlobStoreContainerPath(containerName, null));
        if (container.Exists(blobName))
        {
            var file = container.GetBinary(blobName);
            if (!file.HasValue)
            {
                return Task.FromResult<Result<Optional<Blob>, Error>>(Optional<Blob>.None);
            }

            stream.Write(file.Value.Data);
            return Task.FromResult<Result<Optional<Blob>, Error>>(new Blob
            {
                ContentType = file.Value.ContentType
            }.ToOptional());
        }

        return Task.FromResult<Result<Optional<Blob>, Error>>(Optional<Blob>.None);
    }

    public Task<Result<Error>> UploadAsync(string containerName, string blobName, string contentType, Stream stream,
        CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        blobName.ThrowIfNotValuedParameter(nameof(blobName), Resources.InProcessInMemDataStore_MissingBlobName);
        contentType.ThrowIfNotValuedParameter(nameof(contentType),
            Resources.InProcessInMemDataStore_MissingContentType);
        ArgumentNullException.ThrowIfNull(stream);

        var container = EnsureContainer(GetBlobStoreContainerPath(containerName, null));

        container.AddBinary(blobName, contentType, stream);

        return Task.FromResult(Result.Ok);
    }

    private static string GetBlobStoreContainerPath(string containerName, string? entityId)
    {
        if (entityId.HasValue())
        {
            return $"{BlobStoreContainerName}/{containerName}/{entityId}";
        }

        return $"{BlobStoreContainerName}/{containerName}";
    }
}

internal class BinaryFile
{
    public required string ContentType { get; set; }

    public required byte[] Data { get; set; }
}

#endif