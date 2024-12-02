using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class SearchAllAPIKeysResponse : SearchResponse
{
    public List<APIKey> Keys { get; set; } = [];
}