using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class GetOAuth2ClientResponse : IWebResponse
{
    public required OAuth2Client Client { get; set; }
}