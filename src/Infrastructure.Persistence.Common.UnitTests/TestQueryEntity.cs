using Application.Persistence.Interfaces;
using Common;
using QueryAny;

namespace Infrastructure.Persistence.Common.UnitTests;

[EntityName("acontainername")]
public class TestQueryEntity : IQueryableEntity, IHasIdentity
{
    public bool ABooleanValue { get; set; }

    public double ADoubleValue { get; set; }

    internal string AnInternalProperty { get; set; } = null!;

    public string AStringValue { get; set; } = null!;

    public Optional<bool> IsDeleted { get; set; } = Optional<bool>.None;

    public Optional<DateTime> LastPersistedAtUtc { get; } = Optional<DateTime>.None;

    public Optional<string> Id { get; set; }
}