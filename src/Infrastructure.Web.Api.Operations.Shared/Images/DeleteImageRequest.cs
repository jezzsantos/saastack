using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Images;

/// <summary>
///     Deletes the image
/// </summary>
[Route("/images/{Id}", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class DeleteImageRequest : UnTenantedDeleteRequest
{
    public required string Id { get; set; }
}