using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.UserProfiles;

[Route("/profiles/me", OperationMethod.Get)]
public class GetProfileForCallerRequest : UnTenantedRequest<GetProfileForCallerResponse>
{
}