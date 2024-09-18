using Application.Persistence.Common;
using Common;
using Domain.Shared.EndUsers;
using QueryAny;

namespace EndUsersApplication.Persistence.ReadModels;

[EntityName("Invitation")]
public class Invitation : ReadModelEntity
{
    public Optional<DateTime> AcceptedAt { get; set; }

    public Optional<string> AcceptedEmailAddress { get; set; }

    public Optional<string> InvitedById { get; set; }

    public Optional<string> InvitedEmailAddress { get; set; }

    public Optional<UserStatus> Status { get; set; }

    public Optional<string> Token { get; set; }
}