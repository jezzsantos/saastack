using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars/{id}", ServiceOperation.Delete)]
public class DeleteCarRequest : TenantedDeleteRequest
{
    public required string Id { get; set; }
}