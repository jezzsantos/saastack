using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Verifies that the password reset attempt is still valid
/// </summary>
[Route("/passwords/{Token}/reset/verify", OperationMethod.Get)]
public class VerifyPasswordResetRequest : UnTenantedEmptyRequest<VerifyPasswordResetRequest>
{
    [Required] public string? Token { get; set; }
}