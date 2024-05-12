using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

/// <summary>
///     Registers a new car
/// </summary>
[Route("/cars", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class RegisterCarRequest : TenantedRequest<GetCarResponse>
{
    [Required] public string? Jurisdiction { get; set; }

    [Required] public string? Make { get; set; }

    [Required] public string? Model { get; set; }

    [Required] public string? NumberPlate { get; set; }

    [Required] public int Year { get; set; }
}