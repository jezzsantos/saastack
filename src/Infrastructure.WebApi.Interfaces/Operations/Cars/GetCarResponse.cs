namespace Infrastructure.WebApi.Interfaces.Operations.Cars;

public class GetCarResponse : IWebResponse
{
    public string? Car { get; set; }
}