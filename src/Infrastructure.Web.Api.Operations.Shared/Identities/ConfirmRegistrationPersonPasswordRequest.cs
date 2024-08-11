using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Confirms the invitation to register a new person (verifying their email address)
/// </summary>
[Route("/passwords/confirm-registration", OperationMethod.Post)]
public class ConfirmRegistrationPersonPasswordRequest : UnTenantedRequest<ConfirmRegistrationPersonPasswordRequest,
    ConfirmRegistrationPersonPasswordResponse>
{
    [Required] public string? Token { get; set; }
}