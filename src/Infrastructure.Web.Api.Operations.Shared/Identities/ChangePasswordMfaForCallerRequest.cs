using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Changes whether the user is MFA enabled or not
/// </summary>
[Route("/passwords/mfa", OperationMethod.PutPatch, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class
    ChangePasswordMfaForCallerRequest : UnTenantedRequest<ChangePasswordMfaForCallerRequest, ChangePasswordMfaResponse>
{
    [Required] public bool IsEnabled { get; set; }
}