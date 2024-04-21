using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.UserProfiles;

public class GetProfileForCallerResponse : IWebResponse
{
    public UserProfileForCaller? Profile { get; set; }
}