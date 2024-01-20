using Application.Persistence.Interfaces;
using Common;

namespace Infrastructure.Persistence.Interfaces.UnitTests;

public class TestReadModel : IReadModelEntity
{
    public bool ABooleanValue { get; set; }

    public DateTime ADateTimeValue { get; set; }

    public int AnIntegerValue { get; set; }

    public Optional<DateTime> AnOptionalDateTime { get; set; }

    public Optional<DateTime?> AnOptionalNullableDateTime { get; set; }

    public Optional<string?> AnOptionalNullableString { get; set; }

    public Optional<string> AnOptionalString { get; set; }

    public Optional<TestValueObject> AnOptionalValueObject { get; set; } = null!;

    public bool? ANullableBoolean { get; set; }

    public DateTime? ANullableDateTime { get; set; }

    public int? ANullableInteger { get; set; }

    public string? ANullableString { get; set; }

    public string AStringValue { get; set; } = null!;

    public TestValueObject AValueObject { get; set; } = null!;

    public string Id { get; set; } = null!;

    Optional<string> IHasIdentity.Id
    {
        get => Id;
        set => Id = value;
    }

    public Optional<bool> IsDeleted { get; } = Optional<bool>.None;

    public Optional<DateTime> LastPersistedAtUtc { get; } = Optional<DateTime>.None;
}