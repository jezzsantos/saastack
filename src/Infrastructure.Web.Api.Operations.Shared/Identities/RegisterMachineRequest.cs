using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

[Route("/machines/register", ServiceOperation.Post)]
public class RegisterMachineRequest : UnTenantedRequest<RegisterMachineResponse>
{
    public DateTime? ApiKeyExpiresOnUtc { get; set; }

    public string? CountryCode { get; set; }

    public required string Name { get; set; }

    public string? Timezone { get; set; }
}