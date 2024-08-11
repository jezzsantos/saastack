using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Completes a password reset attempt
/// </summary>
[Route("/passwords/{Token}/reset/complete", OperationMethod.Post)]
public class CompletePasswordResetRequest : UnTenantedEmptyRequest<CompletePasswordResetRequest>
{
    [Required] public string? Password { get; set; }

    [Required] public string? Token { get; set; }
}