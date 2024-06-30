using Application.Resources.Shared;
using ImagesApplication;
using ImagesDomain;
using Infrastructure.Interfaces;
using Infrastructure.Web.Api.Common.Extensions;
using Infrastructure.Web.Api.Interfaces;
using Infrastructure.Web.Api.Operations.Shared.Images;
using Microsoft.AspNetCore.Http;

namespace ImagesInfrastructure.Api.Images;

public class ImagesApi : IWebApiService
{
    private readonly IImagesApplication _application;
    private readonly ICallerContextFactory _callerFactory;
    private readonly IFileUploadService _fileUploadService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ImagesApi(IHttpContextAccessor httpContextAccessor, IFileUploadService fileUploadService,
        ICallerContextFactory callerFactory, IImagesApplication application)
    {
        _httpContextAccessor = httpContextAccessor;
        _fileUploadService = fileUploadService;
        _callerFactory = callerFactory;
        _application = application;
    }

    public async Task<ApiDeleteResult> DeleteImage(DeleteImageRequest request, CancellationToken cancellationToken)
    {
        var image = await _application.DeleteImageAsync(_callerFactory.Create(), request.Id!, cancellationToken);

        return () => image.HandleApplicationResult();
    }

    public async Task<ApiStreamResult> DownloadImage(DownloadImageRequest request,
        CancellationToken cancellationToken)
    {
        var download = await _application.DownloadImageAsync(_callerFactory.Create(), request.Id!, cancellationToken);

        return () => download.HandleApplicationResult(x => new StreamResult(x.Stream, x.ContentType));
    }

    public async Task<ApiGetResult<Image, GetImageResponse>> GetImage(GetImageRequest request,
        CancellationToken cancellationToken)
    {
        var image = await _application.GetImageAsync(_callerFactory.Create(), request.Id!, cancellationToken);

        return () => image.HandleApplicationResult<Image, GetImageResponse>(x => new GetImageResponse { Image = x });
    }

    public async Task<ApiPutPatchResult<Image, UpdateImageResponse>> UpdateImage(UpdateImageRequest request,
        CancellationToken cancellationToken)
    {
        var image = await _application.UpdateImageAsync(_callerFactory.Create(), request.Id!, request.Description,
            cancellationToken);

        return () =>
            image.HandleApplicationResult<Image, UpdateImageResponse>(x => new UpdateImageResponse { Image = x });
    }

    public async Task<ApiPostResult<Image, UploadImageResponse>> UploadImage(UploadImageRequest request,
        CancellationToken cancellationToken)
    {
        var httpRequest = _httpContextAccessor.HttpContext!.Request;
        var uploaded = httpRequest.GetUploadedFile(_fileUploadService, Validations.Images.MaxSizeInBytes,
            Validations.Images.AllowableContentTypes);
        if (uploaded.IsFailure)
        {
            return () => uploaded.Error;
        }

        var image = await _application.UploadImageAsync(_callerFactory.Create(), uploaded.Value, request.Description,
            cancellationToken);

        return () =>
            image.HandleApplicationResult<Image, UploadImageResponse>(x =>
                new PostResult<UploadImageResponse>(new UploadImageResponse { Image = x }));
    }
}