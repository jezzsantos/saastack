using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.UserProfiles;

/// <summary>
///     Gets the profile of the authenticated user
/// </summary>
[Route("/profiles/me", OperationMethod.Get)]
public class GetProfileForCallerRequest : UnTenantedRequest<GetProfileForCallerResponse>
{
}