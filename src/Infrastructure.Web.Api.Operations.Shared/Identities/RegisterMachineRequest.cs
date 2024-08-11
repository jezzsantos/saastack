using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

/// <summary>
///     Register a new machine user on the platform
/// </summary>
[Route("/machines/register", OperationMethod.Post)]
public class RegisterMachineRequest : UnTenantedRequest<RegisterMachineRequest, RegisterMachineResponse>
{
    public DateTime? ApiKeyExpiresOnUtc { get; set; }

    public string? CountryCode { get; set; }

    [Required] public string? Name { get; set; }

    public string? Timezone { get; set; }
}