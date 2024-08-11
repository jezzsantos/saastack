using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.UserProfiles;

/// <summary>
///     Fetches the profile of the authenticated user
/// </summary>
[Route("/profiles/me", OperationMethod.Get)]
public class GetProfileForCallerRequest : UnTenantedRequest<GetProfileForCallerRequest, GetProfileForCallerResponse>
{
}