using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class SearchAllOAuth2ClientsResponse : SearchResponse
{
    public List<OAuth2Client> Clients { get; set; } = [];
}