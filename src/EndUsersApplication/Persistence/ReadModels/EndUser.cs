using Application.Persistence.Common;
using Common;
using Domain.Shared;
using QueryAny;

namespace EndUsersApplication.Persistence.ReadModels;

[EntityName("EndUser")]
public class EndUser : ReadModelEntity
{
    public Optional<string> Access { get; set; }

    public Optional<string> Classification { get; set; }

    public Optional<FeatureLevels> FeatureLevels { get; set; }

    public Optional<Roles> Roles { get; set; }

    public Optional<string> Status { get; set; }

    public Optional<string> Username { get; set; }
}