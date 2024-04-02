using Application.Persistence.Common;
using Common;
using Domain.Shared;
using QueryAny;

namespace EndUsersApplication.Persistence.ReadModels;

[EntityName("Membership")]
public class MembershipJoinInvitation : ReadModelEntity
{
    public Optional<string> UserId { get; set; }

    public Optional<string> OrganizationId { get; set; }

    public bool IsDefault { get; set; }

    public Optional<Roles> Roles { get; set; }

    public Optional<Features> Features { get; set; }

    public Optional<string> InvitedEmailAddress { get; set; }

    public Optional<string> Status { get; set; }
}