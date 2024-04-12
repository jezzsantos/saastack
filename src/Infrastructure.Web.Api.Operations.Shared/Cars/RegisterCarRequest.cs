using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

[Route("/cars", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class RegisterCarRequest : TenantedRequest<GetCarResponse>
{
    public required string Jurisdiction { get; set; }

    public required string Make { get; set; }

    public required string Model { get; set; }

    public required string NumberPlate { get; set; }

    public required int Year { get; set; }
}