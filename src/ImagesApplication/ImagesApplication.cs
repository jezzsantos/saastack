using Application.Common.Extensions;
using Application.Interfaces;
using Application.Interfaces.Services;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using ImagesApplication.Persistence;
using ImagesDomain;

namespace ImagesApplication;

public class ImagesApplication : IImagesApplication
{
    private readonly IHostSettings _hostSettings;
    private readonly IIdentifierFactory _idFactory;
    private readonly IRecorder _recorder;
    private readonly IImagesRepository _repository;

    public ImagesApplication(IRecorder recorder, IIdentifierFactory idFactory, IHostSettings hostSettings,
        IImagesRepository repository)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _hostSettings = hostSettings;
        _repository = repository;
    }

    public async Task<Result<Error>> DeleteImageAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var image = retrieved.Value;
        var deleted = image.Delete(caller.ToCallerId());
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Image {Id} was deleted", image.Id);

        return Result.Ok;
    }

    public async Task<Result<ImageDownload, Error>> DownloadImageAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var image = retrieved.Value;

        //Note: We cannot dispose this stream!
        //It will ultimately be disposed by FileStreamHttpResult.Execute() after the response is written
        var content = new MemoryStream(); //HACK: possibly a better way to buffer this data in memory?
        var downloaded = await _repository.DownloadImageAsync(image.Id, content, cancellationToken);
        if (downloaded.IsFailure)
        {
            return downloaded.Error;
        }

        content.Rewind();

        _recorder.TraceInformation(caller.ToCall(), "Image {Id} was downloaded", image.Id);

        return new ImageDownload
        {
            Stream = content,
            ContentType = image.ContentType
        };
    }

    public async Task<Result<Image, Error>> GetImageAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var image = retrieved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Image {Id} was retrieved", image.Id);

        return image.ToImage(_hostSettings);
    }

    public async Task<Result<Image, Error>> UpdateImageAsync(ICallerContext caller, string id, string? description,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        var image = retrieved.Value;
        if (description.HasValue())
        {
            var detailed = image.ChangeDetails(description);
            if (detailed.IsFailure)
            {
                return detailed.Error;
            }

            var saved = await _repository.SaveAsync(image, cancellationToken);
            if (saved.IsFailure)
            {
                return saved.Error;
            }

            image = saved.Value;
            _recorder.TraceInformation(caller.ToCall(), "Image {Id} was updated", image.Id);
        }

        return image.ToImage(_hostSettings);
    }

    public async Task<Result<Image, Error>> UploadImageAsync(ICallerContext caller, FileUpload upload,
        string? description, CancellationToken cancellationToken)
    {
        var created = ImageRoot.Create(_recorder, _idFactory, upload.ContentType.MediaType!);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var image = created.Value;
        var attributed = image.SetAttributes(upload.Size);
        if (attributed.IsFailure)
        {
            return attributed.Error;
        }

        if (description.HasValue())
        {
            var detailed = image.ChangeDetails(description, upload.Filename);
            if (detailed.IsFailure)
            {
                return detailed.Error;
            }
        }

        var uploaded =
            await _repository.UploadImageAsync(image.Id, upload.ContentType.MediaType!, upload.Content,
                cancellationToken);
        if (uploaded.IsFailure)
        {
            return uploaded.Error;
        }

        var saved = await _repository.SaveAsync(image, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        image = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Image {Id} was uploaded", image.Id);

        return image.ToImage(_hostSettings);
    }
}

internal static class ImageConversionExtensions
{
    public static Image ToImage(this ImageRoot image, IHostSettings hostSettings)
    {
        return new Image
        {
            Id = image.Id,
            Description = image.Description,
            Filename = image.Filename,
            ContentType = image.ContentType,
            Url = hostSettings.MakeImagesApiGetUrl(image.Id)
        };
    }
}