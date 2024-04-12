using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars/{id}", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class DeleteCarRequest : TenantedDeleteRequest
{
    public required string Id { get; set; }
}