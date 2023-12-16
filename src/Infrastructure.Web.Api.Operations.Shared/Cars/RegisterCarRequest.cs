using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars", ServiceOperation.Post)]
public class RegisterCarRequest : TenantedRequest<GetCarResponse>
{
    public required string Jurisdiction { get; set; }

    public required string Make { get; set; }

    public required string Model { get; set; }

    public required string NumberPlate { get; set; }

    public required int Year { get; set; }
}