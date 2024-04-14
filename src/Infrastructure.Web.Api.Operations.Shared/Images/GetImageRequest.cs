using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Images;

[Route("/images/{Id}", OperationMethod.Get)]
public class GetImageRequest : UnTenantedRequest<GetImageResponse>
{
    public required string Id { get; set; }
}