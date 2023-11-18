using Application.Persistence.Interfaces;
using Common;

namespace Application.Persistence.Common;

/// <summary>
///     Provides a read model
/// </summary>
public class ReadModelEntity : IReadModelEntity
{
    public Optional<string> Id { get; set; }

    public Optional<bool> IsDeleted { get; set; }

    public Optional<DateTime> LastPersistedAtUtc { get; set; }
}