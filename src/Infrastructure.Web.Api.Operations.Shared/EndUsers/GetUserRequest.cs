#if TESTINGONLY
using System.ComponentModel.DataAnnotations;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

/// <summary>
///     Fetches the specified user
/// </summary>
[Route("/users/{Id}", OperationMethod.Get, isTestingOnly: true)]
public class GetUserRequest : UnTenantedRequest<GetUserResponse>
{
    [Required] public string? Id { get; set; }
}
#endif