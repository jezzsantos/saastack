using Application.Resources.Shared;
using Infrastructure.Web.Api.Interfaces;

namespace Infrastructure.Web.Api.Operations.Shared.EndUsers;

public class VerifyGuestInvitationResponse : IWebResponse
{
    public required Invitation Invitation { get; set; }
}