using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/machines/register", OperationMethod.Post)]
public class RegisterMachineRequest : UnTenantedRequest<RegisterMachineResponse>
{
    public DateTime? ApiKeyExpiresOnUtc { get; set; }

    public string? CountryCode { get; set; }

    [Required] public string? Name { get; set; }

    public string? Timezone { get; set; }
}