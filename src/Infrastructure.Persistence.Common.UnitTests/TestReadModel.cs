using Application.Persistence.Common;
using QueryAny;

namespace Infrastructure.Persistence.Common.UnitTests;

[EntityName("acontainername")]
public class TestReadModel : ReadModelEntity
{
    public string APropertyName { get; set; } = null!;
}