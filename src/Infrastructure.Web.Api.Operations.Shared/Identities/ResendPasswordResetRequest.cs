using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Resends a password reset attempt (via email)
/// </summary>
[Route("/passwords/{Token}/reset/resend", OperationMethod.Post)]
public class ResendPasswordResetRequest : UnTenantedEmptyRequest<ResendPasswordResetRequest>
{
    [Required] public string? Token { get; set; }
}