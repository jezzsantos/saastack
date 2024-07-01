using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Signings;

/// <summary>
///     Creates a new draft signing request
/// </summary>
[Route("/signingrequests", OperationMethod.Post, AccessType.Token)]
[Authorize(Roles.Tenant_Member, Features.Tenant_PaidTrial)]
public class CreateDraftSigningRequestRequest : TenantedRequest<CreateDraftSigningRequestResponse>
{
    public List<Signee> Signees { get; set; } = new();
}