using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Images;

/// <summary>
///     Uploads a new image. Can be one of the following types: jpg, jpeg, png, gif
/// </summary>
[Route("/images", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class UploadImageRequest : UnTenantedRequest<UploadImageRequest, UploadImageResponse>, IHasMultipartForm
{
    // Will also include bytes for the multipart-form image
    public string? Description { get; set; }
}