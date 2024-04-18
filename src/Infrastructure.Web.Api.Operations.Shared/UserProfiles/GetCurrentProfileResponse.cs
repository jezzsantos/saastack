using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.UserProfiles;

public class GetCurrentProfileResponse : IWebResponse
{
    public UserProfileForCurrent? Profile { get; set; }
}