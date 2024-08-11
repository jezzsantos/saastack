using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Images;

/// <summary>
///     Fetches the details about the image
/// </summary>
[Route("/images/{Id}", OperationMethod.Get)]
public class GetImageRequest : UnTenantedRequest<GetImageRequest, GetImageResponse>
{
    [Required] public string? Id { get; set; }
}