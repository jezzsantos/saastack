using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

/// <summary>
///     List the memberships for the authenticated user
/// </summary>
[Route("/memberships/me", OperationMethod.Search, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_Basic)]
public class
    ListMembershipsForCallerRequest : UnTenantedSearchRequest<ListMembershipsForCallerRequest,
    ListMembershipsForCallerResponse>
{
}