namespace Infrastructure.Web.Api.Interfaces.Operations.Cars;

[Route("/cars/{id}", ServiceOperation.Get)]
public class GetCarRequest : TenantedRequest<GetCarResponse>
{
    public required string Id { get; set; }
}