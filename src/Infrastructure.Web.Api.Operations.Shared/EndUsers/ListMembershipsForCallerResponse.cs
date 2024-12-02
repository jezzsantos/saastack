using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

public class ListMembershipsForCallerResponse : SearchResponse
{
    public List<Membership> Memberships { get; set; } = [];
}