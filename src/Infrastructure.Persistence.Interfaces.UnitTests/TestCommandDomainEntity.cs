using Common;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;

namespace Infrastructure.Persistence.Interfaces.UnitTests;

public class TestCommandDomainAggregateRoot : TestCommandDomainEntity, IDehydratableAggregateRoot;

public class TestCommandDomainEntity : IDehydratableEntity
{
    public required bool ABooleanValue { get; set; }

    public required DateTime ADateTimeValue { get; set; }

    public required int AnIntegerValue { get; set; }

    public required Optional<DateTime> AnOptionalDateTime { get; set; }

    public required Optional<DateTime?> AnOptionalNullableDateTime { get; set; }

    public required Optional<string?> AnOptionalNullableString { get; set; }

    public required Optional<string> AnOptionalString { get; set; }

    public required Optional<TestValueObject> AnOptionalValueObject { get; set; } = null!;

    public bool? ANullableBoolean { get; set; }

    public DateTime? ANullableDateTime { get; set; }

    public int? ANullableInteger { get; set; }

    public string? ANullableString { get; set; }

    public required string AStringValue { get; set; }

    public required TestValueObject AValueObject { get; set; } = null!;

    public required string Id { get; set; }

    public HydrationProperties Dehydrate()
    {
        var properties = new HydrationProperties
        {
            { nameof(Id), Id },
            { nameof(AStringValue), AStringValue },
            { nameof(AnIntegerValue), AnIntegerValue },
            { nameof(ABooleanValue), ABooleanValue },
            { nameof(ADateTimeValue), ADateTimeValue },
            { nameof(AnOptionalString), AnOptionalString },
            { nameof(AnOptionalDateTime), AnOptionalDateTime },
            { nameof(AnOptionalNullableString), AnOptionalNullableString },
            { nameof(AnOptionalNullableDateTime), AnOptionalNullableDateTime },
            { nameof(AValueObject), AValueObject },
            { nameof(AnOptionalValueObject), AnOptionalValueObject }
        };

        return properties;
    }

    ISingleValueObject<string> IIdentifiableEntity.Id => Identifier.Create(Id);

    public Optional<bool> IsDeleted { get; } = Optional<bool>.None;

    public Optional<DateTime> LastPersistedAtUtc { get; } = Optional<DateTime>.None;
}