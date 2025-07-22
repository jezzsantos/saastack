using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Fetches the user's info for the authenticated user in OpenId Connect format
/// </summary>
[Route("/oauth2/userinfo", OperationMethod.Get, AccessType.Token)]
[Authorize(Roles.Platform_Standard)]
public class GetUserInfoForCallerRequest : UnTenantedRequest<GetUserInfoForCallerRequest, GetUserInfoForCallerResponse>
{
}