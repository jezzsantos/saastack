using Application.Interfaces.Resources;

namespace Infrastructure.WebApi.Interfaces.Operations.Cars;

public class GetCarResponse : IWebResponse
{
    public required Car Car { get; set; }
}