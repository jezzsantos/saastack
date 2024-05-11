using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/passwords/confirm-registration", OperationMethod.Post)]
public class ConfirmRegistrationPersonPasswordRequest : UnTenantedRequest<ConfirmRegistrationPersonPasswordResponse>
{
    [Required] public string? Token { get; set; }
}