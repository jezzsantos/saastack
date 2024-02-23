using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Organizations;

public class GetOrganizationResponse : IWebResponse
{
    public Organization? Organization { get; set; }
}