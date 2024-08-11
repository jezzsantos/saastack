using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

/// <summary>
///     Creates a new organization to share with other users
/// </summary>
[Route("/organizations", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Platform_Standard, Features.Platform_PaidTrial)]
public class CreateOrganizationRequest : UnTenantedRequest<CreateOrganizationRequest, GetOrganizationResponse>
{
    [Required] public string? Name { get; set; }
}