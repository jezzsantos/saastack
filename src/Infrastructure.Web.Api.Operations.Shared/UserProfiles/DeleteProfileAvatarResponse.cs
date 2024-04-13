using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.UserProfiles;

public class DeleteProfileAvatarResponse : IWebResponse
{
    public UserProfile? Profile { get; set; }
}