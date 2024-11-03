using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class GetIdentityResponse : IWebResponse
{
    public Identity? Identity { get; set; }
}