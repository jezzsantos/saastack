using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

public class UnInviteMemberFromOrganizationResponse : IWebResponse
{
    public Organization? Organization { get; set; }
}