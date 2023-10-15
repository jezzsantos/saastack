namespace Infrastructure.WebApi.Interfaces.Operations.Cars;

[Route("/cars/{id}", ServiceOperation.Delete)]
public class DeleteCarRequest : IWebRequestVoid
{
    public required string Id { get; set; }
}