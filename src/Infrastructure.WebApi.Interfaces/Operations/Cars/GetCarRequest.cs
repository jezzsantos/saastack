namespace Infrastructure.WebApi.Interfaces.Operations.Cars;

public class GetCarRequest : IWebRequest<GetCarResponse>
{
    public string? Id { get; set; }
}