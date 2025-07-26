using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Lists all OAuth2/OIDC clients
/// </summary>
[Route("/oauth2/clients", OperationMethod.Search, AccessType.Token)]
[Authorize(Roles.Platform_Operations)]
public class
    SearchAllOAuth2ClientsRequest : UnTenantedSearchRequest<SearchAllOAuth2ClientsRequest,
    SearchAllOAuth2ClientsResponse>
{
}