using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Infrastructure.Persistence.Interfaces;

namespace Infrastructure.Persistence.Common;

/// <summary>
///     Provides read/write access to binary blobs
/// </summary>
public sealed class BinaryBlobStore : IBinaryBlobStore
{
    private readonly IBlobStore _blobStore;
    private readonly string _containerName;
    private readonly IRecorder _recorder;

    public BinaryBlobStore(IRecorder recorder, string containerName, IBlobStore blobStore)
    {
        containerName.ThrowIfNotValuedParameter(nameof(containerName), Resources.AnyStore_EmptyContainerName);

        InstanceId = Guid.NewGuid();
        _recorder = recorder;
        _blobStore = blobStore;
        _containerName = containerName;
    }

    public Guid InstanceId { get; }

#if TESTINGONLY
    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        var deleted = await _blobStore.DestroyAllAsync(_containerName, cancellationToken);
        if (deleted.IsSuccessful)
        {
            _recorder.TraceDebug(null, "All blobs were deleted from the container {Container} in the {Store} store",
                _containerName, _blobStore.GetType().Name);
        }

        return deleted;
    }
#endif

    public async Task<Result<Error>> DestroyAsync(string blobName, CancellationToken cancellationToken)
    {
        if (blobName.IsNotValuedParameter(nameof(blobName), out var error))
        {
            return error;
        }

        var deleted = await _blobStore.DeleteAsync(_containerName, blobName, cancellationToken);
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        _recorder.TraceDebug(null, "Blob {Name} was deleted from the container {Container} in the {Store} store",
            blobName, _containerName, _blobStore.GetType()
                .Name);

        return Result.Ok;
    }

    public async Task<Result<Optional<Blob>, Error>> GetAsync(string blobName, Stream stream,
        CancellationToken cancellationToken)
    {
        if (blobName.IsNotValuedParameter(nameof(blobName), out var error))
        {
            return error;
        }

        var download = await _blobStore.DownloadAsync(_containerName, blobName, stream, cancellationToken);
        if (download.IsFailure)
        {
            return download.Error;
        }

        var blob = download.Value;
        _recorder.TraceDebug(null, "Blob {Name} was retrieved from the container {Container} in the {Store} store",
            blobName, _containerName, _blobStore.GetType().Name);

        return blob;
    }

    public async Task<Result<Error>> SaveAsync(string blobName, string contentType, Stream stream,
        CancellationToken cancellationToken)
    {
        if (blobName.IsNotValuedParameter(nameof(blobName), out var error1))
        {
            return error1;
        }

        if (contentType.IsNotValuedParameter(nameof(contentType), Resources.BlobStore_EmptyContentType,
                out var error2))
        {
            return error2;
        }

        var upload = await _blobStore.UploadAsync(_containerName, blobName, contentType, stream, cancellationToken);
        if (upload.IsFailure)
        {
            return upload.Error;
        }

        _recorder.TraceDebug(null, "Blob {Name} was saved to the container {Container} in the {Store} store", blobName,
            _containerName, _blobStore.GetType().Name);

        return Result.Ok;
    }
}