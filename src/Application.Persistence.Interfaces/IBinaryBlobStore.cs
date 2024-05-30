using Common;

namespace Application.Persistence.Interfaces;

/// <summary>
///     Defines a store for reading and writing binary blob data
/// </summary>
public interface IBinaryBlobStore
{
#if TESTINGONLY
    /// <summary>
    ///     Permanently destroys all blobs in the store
    /// </summary>
    Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken);
#endif

    /// <summary>
    ///     Permanently destroys the existing blob from the store.
    /// </summary>
    Task<Result<Error>> DestroyAsync(string blobName, CancellationToken cancellationToken);

    /// <summary>
    ///     Retrieves the existing blob from the store, and writes the blob to the specified <see cref="stream" />
    ///     at its current position.
    /// </summary>
    Task<Result<Optional<Blob>, Error>> GetAsync(string blobName, Stream stream, CancellationToken cancellationToken);

    /// <summary>
    ///     Updates an existing blob or inserts a new blob into the store, by reading from the specified <see cref="stream" />
    ///     as its current position.
    /// </summary>
    Task<Result<Error>> SaveAsync(string blobName, string contentType, Stream stream,
        CancellationToken cancellationToken);
}