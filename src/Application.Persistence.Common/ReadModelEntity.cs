using Application.Persistence.Interfaces;

namespace Application.Persistence.Common;

/// <summary>
///     Provides a read model
/// </summary>
public class ReadModelEntity : IReadModelEntity
{
    public required string Id { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? LastPersistedAtUtc { get; set; }
}