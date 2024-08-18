#if TESTINGONLY
using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Interfaces;
using Infrastructure.Persistence.Interfaces;
using Task = System.Threading.Tasks.Task;

namespace Infrastructure.Persistence.Shared.ApplicationServices;

partial class InProcessInMemStore : IBlobStore
{
    private readonly Dictionary<string, Dictionary<string, HydrationProperties>> _blobs = new();

    public Task<Result<Error>> DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        blobName.ThrowIfNotValuedParameter(nameof(blobName), Resources.InProcessInMemDataStore_MissingBlobName);

        if (_blobs.ContainsKey(containerName)
            && _blobs[containerName].ContainsKey(blobName))
        {
            _blobs[containerName].Remove(blobName);
        }

        return Task.FromResult(Result.Ok);
    }

#if TESTINGONLY
    Task<Result<Error>> IBlobStore.DestroyAllAsync(string containerName, CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);

        if (_blobs.ContainsKey(containerName))
        {
            _blobs.Remove(containerName);
        }

        return Task.FromResult(Result.Ok);
    }
#endif

    public Task<Result<Optional<Blob>, Error>> DownloadAsync(string containerName, string blobName, Stream stream,
        CancellationToken cancellationToken)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName),
            Resources.AnyStore_MissingContainerName);
        blobName.ThrowIfNotValuedParameter(nameof(blobName), Resources.InProcessInMemDataStore_MissingBlobName);
        ArgumentNullException.ThrowIfNull(stream);

        if (_blobs.ContainsKey(containerName)
            && _blobs[containerName].ContainsKey(blobName))
        {
            var properties = _blobs[containerName][blobName];
            var data = (properties["Data"].ValueOrDefault ?? string.Empty).ToString()!;
            stream.Write(Convert.FromBase64String(data));
            return Task.FromResult<Result<Optional<Blob>, Error>>(new Blob
            {
                ContentType = properties[nameof(Blob.ContentType)].ToString()
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

        if (!_blobs.ContainsKey(containerName))
        {
            _blobs.Add(containerName, new Dictionary<string, HydrationProperties>());
        }

        var properties = new HydrationProperties
        {
            { "ContentType", contentType },
            { "Data", Convert.ToBase64String(stream.ReadFully()) }
        };
        if (_blobs[containerName].ContainsKey(blobName))
        {
            _blobs[containerName][blobName] = properties;
            return Task.FromResult(Result.Ok);
        }

        _blobs[containerName].Add(blobName, properties);

        return Task.FromResult(Result.Ok);
    }
}
#endif