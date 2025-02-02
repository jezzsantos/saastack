using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

/// <summary>
///     Fetches the specified user
/// </summary>
[Route("/users/{Id}", OperationMethod.Get, AccessType.PrivateInterHost)]
public class GetUserPrivateRequest : UnTenantedRequest<GetUserPrivateRequest, GetUserPrivateResponse>
{
    [Required] public string? Id { get; set; }
}