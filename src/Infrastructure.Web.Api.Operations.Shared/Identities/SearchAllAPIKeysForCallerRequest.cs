using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Lists all the API keys for the authenticated user
/// </summary>
[Route("/apikeys/me", OperationMethod.Search, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class
    SearchAllAPIKeysForCallerRequest : TenantedSearchRequest<SearchAllAPIKeysForCallerRequest, SearchAllAPIKeysResponse>
{
}