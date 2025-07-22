using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class GetJsonWebKeySetResponse : IWebResponse
{
    public required JsonWebKeySet Keys { get; set; }
}