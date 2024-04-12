using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars/{id}", OperationMethod.Get, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_Basic)]
public class GetCarRequest : TenantedRequest<GetCarResponse>
{
    public required string Id { get; set; }
}