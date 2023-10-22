using Application.Interfaces.Resources;

namespace Infrastructure.Web.Api.Interfaces.Operations.Cars;

public class GetCarResponse : IWebResponse
{
    public Car? Car { get; set; }
}