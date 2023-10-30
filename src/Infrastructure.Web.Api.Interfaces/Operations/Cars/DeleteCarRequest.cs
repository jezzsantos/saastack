namespace Infrastructure.Web.Api.Interfaces.Operations.Cars;

[Route("/cars/{id}", ServiceOperation.Delete)]
public class DeleteCarRequest : TenantedDeleteRequest
{
    public required string Id { get; set; }
}