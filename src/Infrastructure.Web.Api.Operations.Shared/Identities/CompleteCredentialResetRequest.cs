using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Completes a password reset attempt
/// </summary>
[Route("/credentials/{Token}/reset/complete", OperationMethod.Post)]
public class CompleteCredentialResetRequest : UnTenantedEmptyRequest<CompleteCredentialResetRequest>
{
    [Required] public string? Password { get; set; }

    [Required] public string? Token { get; set; }
}