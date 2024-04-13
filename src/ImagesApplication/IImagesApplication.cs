using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace ImagesApplication;

public interface IImagesApplication
{
    Task<Result<Error>> DeleteImageAsync(ICallerContext caller, string id, CancellationToken cancellationToken);

    Task<Result<ImageDownload, Error>> DownloadImageAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken);

    Task<Result<Image, Error>> GetImageAsync(ICallerContext caller, string id, CancellationToken cancellationToken);

    Task<Result<Image, Error>> UpdateImageAsync(ICallerContext caller, string id, string? description,
        CancellationToken cancellationToken);

    Task<Result<Image, Error>> UploadImageAsync(ICallerContext caller, FileUpload upload, string? description,
        CancellationToken cancellationToken);
}