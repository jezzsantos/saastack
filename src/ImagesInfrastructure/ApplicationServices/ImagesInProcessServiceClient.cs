using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using ImagesApplication;

namespace ImagesInfrastructure.ApplicationServices;

public class ImagesInProcessServiceClient : IImagesService
{
    private readonly IImagesApplication _imagesApplication;

    public ImagesInProcessServiceClient(IImagesApplication imagesApplication)
    {
        _imagesApplication = imagesApplication;
    }

    public async Task<Result<Image, Error>> CreateImageAsync(ICallerContext caller, FileUpload upload,
        string description,
        CancellationToken cancellationToken)
    {
        return await _imagesApplication.UploadImageAsync(caller, upload, description, cancellationToken);
    }

    public async Task<Result<Error>> DeleteImageAsync(ICallerContext caller, string id,
        CancellationToken cancellationToken)
    {
        return await _imagesApplication.DeleteImageAsync(caller, id, cancellationToken);
    }
}