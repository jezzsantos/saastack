using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/passwords/{Token}/reset/verify", OperationMethod.Get)]
public class VerifyPasswordResetRequest : UnTenantedEmptyRequest
{
    [Required] public string? Token { get; set; }
}