using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

public class ReserveCarIfAvailableResponse : IWebResponse
{
    public bool IsReserved { get; set; }
}