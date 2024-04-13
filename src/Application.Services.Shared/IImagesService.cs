using Application.Interfaces;
using Application.Resources.Shared;
using Common;

namespace Application.Services.Shared;

public interface IImagesService
{
    Task<Result<Image, Error>> CreateImageAsync(ICallerContext caller, FileUpload upload, string description,
        CancellationToken cancellationToken);

    Task<Result<Error>> DeleteImageAsync(ICallerContext caller, string id, CancellationToken cancellationToken);
}