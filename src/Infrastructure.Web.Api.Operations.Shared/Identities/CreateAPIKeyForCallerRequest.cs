using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Creates a new API key for the authenticated user, and expires all other API keys
/// </summary>
[Route("/apikeys", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class CreateAPIKeyForCallerRequest : UnTenantedRequest<CreateAPIKeyForCallerRequest, CreateAPIKeyResponse>
{
    public DateTime? ExpiresOnUtc { get; set; }
}