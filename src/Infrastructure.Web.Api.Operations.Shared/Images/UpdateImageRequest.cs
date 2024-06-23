using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Images;

/// <summary>
///     Changes the image details
/// </summary>
[Route("/images/{Id}", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class UpdateImageRequest : UnTenantedRequest<UpdateImageResponse>
{
    public string? Description { get; set; }

    [Required] public string? Id { get; set; }
}