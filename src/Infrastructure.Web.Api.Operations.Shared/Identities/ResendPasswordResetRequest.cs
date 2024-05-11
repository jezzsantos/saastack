using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/passwords/{Token}/reset/resend", OperationMethod.Post)]
public class ResendPasswordResetRequest : UnTenantedEmptyRequest
{
    [Required] public string? Token { get; set; }
}