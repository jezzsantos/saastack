using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Cars;

/// <summary>
///     Deletes the specified car
/// </summary>
[Route("/cars/{Id}", OperationMethod.Delete, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class DeleteCarRequest : TenantedDeleteRequest
{
    [Required] public string? Id { get; set; }
}