using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Images;

/// <summary>
///     Downloads the image
/// </summary>
[Route("/images/{Id}/download", OperationMethod.Get)]
public class DownloadImageRequest : UnTenantedStreamRequest
{
    public required string Id { get; set; }
}