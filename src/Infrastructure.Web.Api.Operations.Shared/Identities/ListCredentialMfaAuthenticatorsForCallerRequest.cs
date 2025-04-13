using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Fetches the MFA authenticators for the user
/// </summary>
/// <remarks>
///     This API can be called Anonymously (during password authentication), as well as after being authenticated
/// </remarks>
[Route("/credentials/mfa/authenticators", OperationMethod.Get)]
public class ListCredentialMfaAuthenticatorsForCallerRequest : UnTenantedRequest<
    ListCredentialMfaAuthenticatorsForCallerRequest, ListCredentialMfaAuthenticatorsForCallerResponse>
{
    public string? MfaToken { get; set; }
}