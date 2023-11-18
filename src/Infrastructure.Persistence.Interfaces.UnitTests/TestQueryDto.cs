using QueryAny;

namespace Infrastructure.Persistence.Interfaces.UnitTests;

public class TestQueryDto : IQueryableEntity
{
    public bool ABooleanValue { get; set; }

    public DateTime ADateTimeValue { get; set; }

    public int AnIntegerValue { get; set; }

    public bool? ANullableBoolean { get; set; }

    public DateTime? ANullableDateTime { get; set; }

    public int? ANullableInteger { get; set; }

    public string? ANullableString { get; set; }

    public string AStringValue { get; set; } = null!;

    public string Id { get; set; } = null!;
}