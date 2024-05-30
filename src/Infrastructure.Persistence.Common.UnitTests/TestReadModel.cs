using Application.Persistence.Common;
using Common;
using QueryAny;

namespace Infrastructure.Persistence.Common.UnitTests;

[EntityName("acontainername")]
public class TestReadModel : ReadModelEntity
{
    public string AStringValue { get; set; } = null!;

    public Optional<string> AnOptionalStringValue { get; set; }
}