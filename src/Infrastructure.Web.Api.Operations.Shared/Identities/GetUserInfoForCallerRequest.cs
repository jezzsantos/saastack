using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Fetches the user's info for the authenticated user in Open ID Connect format
/// </summary>
/// <response code="403">
///     The token was not authorized using Open ID Connect, or the user is not consented to the client, or the user
///     is unknown or not registered
/// </response>
/// <response code="423">The user's account is suspended or disabled, and cannot be used</response>
[Route("/oauth2/userinfo", OperationMethod.Get, AccessType.Token)]
[Authorize(Roles.Platform_Standard)]
public class GetUserInfoForCallerRequest : UnTenantedRequest<GetUserInfoForCallerRequest, GetUserInfoForCallerResponse>
{
}