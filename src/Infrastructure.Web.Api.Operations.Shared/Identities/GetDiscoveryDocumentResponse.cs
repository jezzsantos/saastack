using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class GetDiscoveryDocumentResponse : IWebResponse
{
    public required OpenIdConnectDiscoveryDocument Document { get; set; }
}