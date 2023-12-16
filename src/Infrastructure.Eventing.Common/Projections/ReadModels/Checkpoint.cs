using Application.Persistence.Common;
using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using QueryAny;

namespace Infrastructure.Eventing.Common.Projections.ReadModels;

/// <summary>
///     Provides a checkpoint for event relaying
/// </summary>
[EntityName("ProjectionCheckpoints")]
public class Checkpoint : ReadModelEntity, IIdentifiableEntity
{
    public Optional<int> Position { get; set; }

    public Optional<string> StreamName { get; set; }

    ISingleValueObject<string> IIdentifiableEntity.Id => Identifier.Create(Id.Value);
}