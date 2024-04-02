using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

public class ListMembersForOrganizationResponse : SearchResponse
{
    public List<OrganizationMember>? Members { get; set; }
}