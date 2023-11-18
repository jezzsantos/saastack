using Application.Persistence.Interfaces;
using Common;
using QueryAny;

namespace Infrastructure.Persistence.Common.UnitTests;

[EntityName("acontainername")]
public class TestDto : IPersistableDto
{
    public string AStringValue { get; set; } = null!;

    public Optional<string> Id { get; set; }

    public Optional<bool> IsDeleted { get; set; }

    public Optional<DateTime> LastPersistedAtUtc { get; set; }
}