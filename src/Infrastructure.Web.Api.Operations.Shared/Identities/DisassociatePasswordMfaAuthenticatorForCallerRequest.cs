using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Disassociates an associated MFA authenticator from the user
/// </summary>
[Route("/passwords/mfa/authenticators/{Id}", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class DisassociatePasswordMfaAuthenticatorForCallerRequest : UnTenantedDeleteRequest<
    DisassociatePasswordMfaAuthenticatorForCallerRequest>
{
    [Required] public string? Id { get; set; }
}