using Application.Persistence.Common;
using Common;
using Domain.Shared;
using Domain.Shared.EndUsers;
using QueryAny;

namespace EndUsersApplication.Persistence.ReadModels;

[EntityName("EndUser")]
public class EndUser : ReadModelEntity
{
    public Optional<UserAccess> Access { get; set; }

    public Optional<UserClassification> Classification { get; set; }

    public Optional<Features> Features { get; set; }

    public Optional<Roles> Roles { get; set; }

    public Optional<UserStatus> Status { get; set; }

    public Optional<string> Username { get; set; }
}