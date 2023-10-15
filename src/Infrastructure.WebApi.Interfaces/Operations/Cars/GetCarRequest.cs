namespace Infrastructure.WebApi.Interfaces.Operations.Cars;

[Route("/cars/{id}", ServiceOperation.Get)]
public class GetCarRequest : IWebRequest<GetCarResponse>
{
    public required string Id { get; set; }
}