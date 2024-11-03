using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Fetches identity details of the user
/// </summary>
[Route("/users/me", OperationMethod.Get, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class GetIdentityForCallerRequest : UnTenantedRequest<GetIdentityForCallerRequest, GetIdentityResponse>
{
}