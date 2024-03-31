using Application.Persistence.Common;
using Common;
using QueryAny;

namespace EndUsersApplication.Persistence.ReadModels;

[EntityName("Invitation")]
public class Invitation : ReadModelEntity
{
    public Optional<DateTime> AcceptedAtUtc { get; set; }

    public Optional<string> AcceptedEmailAddress { get; set; }

    public Optional<string> InvitedById { get; set; }

    public Optional<string> InvitedEmailAddress { get; set; }

    public Optional<string> Status { get; set; }

    public Optional<string> Token { get; set; }
}