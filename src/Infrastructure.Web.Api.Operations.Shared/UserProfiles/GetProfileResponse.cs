using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.UserProfiles;

public class GetProfileResponse : IWebResponse
{
    public required UserProfile Profile { get; set; }
}