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
    private readonly ICallerContextFactory _contextFactory;
    private readonly IFileUploadService _fileUploadService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ImagesApi(IHttpContextAccessor httpContextAccessor, IFileUploadService fileUploadService,
        ICallerContextFactory contextFactory, IImagesApplication application)
    {
        _httpContextAccessor = httpContextAccessor;
        _fileUploadService = fileUploadService;
        _contextFactory = contextFactory;
        _application = application;
    }

    public async Task<ApiStreamResult> DownloadImage(DownloadImageRequest request,
        CancellationToken cancellationToken)
    {
        var download = await _application.DownloadImageAsync(_contextFactory.Create(), request.Id, cancellationToken);

        return () => download.HandleApplicationResult(x => new StreamResult(x.Stream, x.ContentType));
    }

    public async Task<ApiGetResult<Image, GetImageResponse>> GetImage(GetImageRequest request,
        CancellationToken cancellationToken)
    {
        var image = await _application.GetImageAsync(_contextFactory.Create(), request.Id, cancellationToken);

        return () => image.HandleApplicationResult<GetImageResponse, Image>(x => new GetImageResponse { Image = x });
    }

    public async Task<ApiDeleteResult> ImageDelete(DeleteImageRequest request, CancellationToken cancellationToken)
    {
        var image = await _application.DeleteImageAsync(_contextFactory.Create(), request.Id, cancellationToken);

        return () => image.HandleApplicationResult();
    }

    public async Task<ApiPutPatchResult<Image, UpdateImageResponse>> UpdateImage(UpdateImageRequest request,
        CancellationToken cancellationToken)
    {
        var image = await _application.UpdateImageAsync(_contextFactory.Create(), request.Id, request.Description,
            cancellationToken);

        return () =>
            image.HandleApplicationResult<UpdateImageResponse, Image>(x => new UpdateImageResponse { Image = x });
    }

    public async Task<ApiPostResult<Image, UploadImageResponse>> UploadImage(UploadImageRequest request,
        CancellationToken cancellationToken)
    {
        var httpRequest = _httpContextAccessor.HttpContext!.Request;
        var uploaded = httpRequest.GetUploadedFile(_fileUploadService, Validations.Images.MaxSizeInBytes,
            Validations.Images.AllowableContentTypes);
        if (!uploaded.IsSuccessful)
        {
            return () => uploaded.Error;
        }

        var image = await _application.UploadImageAsync(_contextFactory.Create(), uploaded.Value, request.Description,
            cancellationToken);

        return () =>
            image.HandleApplicationResult<UploadImageResponse, Image>(x =>
                new PostResult<UploadImageResponse>(new UploadImageResponse { Image = x }));
    }
}