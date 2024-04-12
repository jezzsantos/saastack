using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Images;

[Route("/images", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class UploadImageRequest : UnTenantedRequest<UploadImageResponse>, IHasMultipartForm
{
    // Will also include bytes for the multipart-form image
    public string? Description { get; set; }
}