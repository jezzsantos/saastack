using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

public class InviteGuestResponse : IWebResponse
{
    public required Invitation Invitation { get; set; }
}