using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

/// <summary>
///     Fetches the specified car
/// </summary>
[Route("/cars/{Id}", OperationMethod.Get, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class GetCarRequest : TenantedRequest<GetCarRequest, GetCarResponse>
{
    [Required] public string? Id { get; set; }
}