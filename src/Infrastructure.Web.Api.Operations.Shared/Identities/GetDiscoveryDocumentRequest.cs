using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Fetches the discovery document for OpenID Connect
/// </summary>
[Route("/.well-known/openid-configuration", OperationMethod.Get)]
public class GetDiscoveryDocumentRequest : UnTenantedRequest<GetDiscoveryDocumentRequest, GetDiscoveryDocumentResponse>
{
}