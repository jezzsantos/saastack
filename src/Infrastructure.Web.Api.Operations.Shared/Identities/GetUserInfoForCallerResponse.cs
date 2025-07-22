using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.Identities;

public class GetUserInfoForCallerResponse : IWebResponse
{
    public required OidcUserInfoResponse OidcUserInfo { get; set; }
}