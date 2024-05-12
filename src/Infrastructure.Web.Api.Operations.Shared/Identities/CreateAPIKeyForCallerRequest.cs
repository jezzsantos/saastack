#if TESTINGONLY
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Create a new API key for the authenticated user
/// </summary>
[Route("/apikeys/me", OperationMethod.Post, AccessType.Token, true)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class CreateAPIKeyForCallerRequest : UnTenantedRequest<CreateAPIKeyResponse>
{
    public DateTime? ExpiresOnUtc { get; set; }
}

#endif