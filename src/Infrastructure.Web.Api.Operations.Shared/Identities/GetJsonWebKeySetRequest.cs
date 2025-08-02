using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Fetches the JSON Web Key Set for Open ID Connect JWT verification
/// </summary>
[Route("/.well-known/jwks.json", OperationMethod.Get)]
public class GetJsonWebKeySetRequest : UnTenantedRequest<GetJsonWebKeySetRequest, GetJsonWebKeySetResponse>
{
}