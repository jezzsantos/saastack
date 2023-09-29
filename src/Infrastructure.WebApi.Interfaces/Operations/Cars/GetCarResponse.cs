using Application.Interfaces.Resources;

namespace Infrastructure.WebApi.Interfaces.Operations.Cars;

public class GetCarResponse : IWebResponse
{
    public Car? Car { get; set; }
}