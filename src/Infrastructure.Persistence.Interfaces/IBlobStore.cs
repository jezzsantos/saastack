using Application.Persistence.Interfaces;
using Common;

namespace Infrastructure.Persistence.Interfaces;

/// <summary>
///     Defines read/write access to individual binary globs of data to and from a blob store
///     (e.g. a blob store or a file system)
/// </summary>
public interface IBlobStore
{
    Task<Result<Error>> DeleteAsync(string containerName, string blobName, CancellationToken cancellationToken);

#if TESTINGONLY
    Task<Result<Error>> DestroyAllAsync(string containerName, CancellationToken cancellationToken);
#endif

    Task<Result<Optional<Blob>, Error>> DownloadAsync(string containerName, string blobName, Stream stream,
        CancellationToken cancellationToken);

    Task<Result<Error>> UploadAsync(string containerName, string blobName, string contentType, Stream stream,
        CancellationToken cancellationToken);
}