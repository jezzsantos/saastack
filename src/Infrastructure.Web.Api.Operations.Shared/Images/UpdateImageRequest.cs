using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Images;

/// <summary>
///     Changes the image details
/// </summary>
[Route("/images/{Id}", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class UpdateImageRequest : UnTenantedRequest<UpdateImageResponse>
{
    public string? Description { get; set; }

    public required string Id { get; set; }
}