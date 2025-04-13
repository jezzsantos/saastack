using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Confirms the invitation to register a new person (verifying their email address)
/// </summary>
[Route("/credentials/confirm-registration", OperationMethod.Post)]
public class ConfirmRegistrationPersonCredentialRequest : UnTenantedRequest<ConfirmRegistrationPersonCredentialRequest,
    ConfirmRegistrationPersonCredentialResponse>
{
    [Required] public string? Token { get; set; }
}