using Application.Persistence.Interfaces;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using ImagesApplication.Persistence;
using ImagesApplication.Persistence.ReadModels;
using ImagesDomain;
using Infrastructure.Persistence.Common;
using Infrastructure.Persistence.Interfaces;

namespace ImagesInfrastructure.Persistence;

public class ImagesRepository : IImagesRepository
{
    private const string ImageContainerName = "images";
    private readonly IBinaryBlobStore _imageBlobs;
    private readonly ISnapshottingQueryStore<Image> _imageQueries;
    private readonly IEventSourcingDddCommandStore<ImageRoot> _images;

    public ImagesRepository(IRecorder recorder, IDomainFactory domainFactory,
        IEventSourcingDddCommandStore<ImageRoot> imagesStore, IBlobStore blobStore, IDataStore dataStore)
    {
        _imageQueries = new SnapshottingQueryStore<Image>(recorder, domainFactory, dataStore);
        _images = imagesStore;
        _imageBlobs = new BinaryBlobStore(recorder, ImageContainerName, blobStore);
    }

    public async Task<Result<Error>> DestroyAllAsync(CancellationToken cancellationToken)
    {
        return await Tasks.WhenAllAsync(
            _imageQueries.DestroyAllAsync(cancellationToken),
            _images.DestroyAllAsync(cancellationToken),
            _imageBlobs.DestroyAllAsync(cancellationToken));
    }

    public async Task<Result<ImageRoot, Error>> LoadAsync(Identifier id, CancellationToken cancellationToken)
    {
        var image = await _images.LoadAsync(id, cancellationToken);
        if (image.IsFailure)
        {
            return image.Error;
        }

        return image;
    }

    public async Task<Result<ImageRoot, Error>> SaveAsync(ImageRoot profile,
        CancellationToken cancellationToken)
    {
        var saved = await _images.SaveAsync(profile, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        return profile;
    }

    public async Task<Result<Blob, Error>> DownloadImageAsync(Identifier id, Stream content,
        CancellationToken cancellationToken)
    {
        var blobName = id.ToString();
        var blob = await _imageBlobs.GetAsync(blobName, content, cancellationToken);
        if (blob.IsFailure)
        {
            return blob.Error;
        }

        if (!blob.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        return blob.Value.Value;
    }

    public async Task<Result<Error>> UploadImageAsync(Identifier id, string contentType, Stream content,
        CancellationToken cancellationToken)
    {
        var blobName = id;
        return await _imageBlobs.SaveAsync(blobName, contentType, content, cancellationToken);
    }
}