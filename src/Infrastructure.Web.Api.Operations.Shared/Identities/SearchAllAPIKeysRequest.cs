using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/apikeys", OperationMethod.Search, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class SearchAllAPIKeysRequest : TenantedSearchRequest<SearchAllAPIKeysResponse>
{
}