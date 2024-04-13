using Application.Persistence.Interfaces;
using Common;
using Domain.Common.ValueObjects;
using ImagesDomain;

namespace ImagesApplication.Persistence;

public interface IImagesRepository : IApplicationRepository
{
    Task<Result<Blob, Error>> DownloadImageAsync(Identifier id, Stream content, CancellationToken cancellationToken);

    Task<Result<ImageRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken);

    Task<Result<ImageRoot, Error>> SaveAsync(ImageRoot image, CancellationToken cancellationToken);

    Task<Result<Error>> UploadImageAsync(Identifier id, string contentType, Stream content,
        CancellationToken cancellationToken);
}