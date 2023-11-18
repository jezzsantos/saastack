using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Entities;
using Domain.Interfaces.ValueObjects;
using QueryAny;

namespace Infrastructure.Persistence.Common.UnitTests;

[EntityName("acontainername")]
public class TestCommandEntity : IDehydratableEntity
{
    public TestCommandEntity(string identifier)
    {
        Id = identifier.ToId();
    }

    public bool ABooleanValue { get; set; }

    public double ADoubleValue { get; set; }

    internal string AnInternalProperty { get; set; } = null!;

    public string AStringValue { get; set; } = null!;

    public void Rehydrate(HydrationProperties properties)
    {
        this.PopulateWith(properties.ToObjectDictionary());
    }

    public Optional<DateTime> LastPersistedAtUtc { get; } = Optional<DateTime>.None;

    public Optional<bool> IsDeleted { get; set; } = Optional<bool>.None;

    public ISingleValueObject<string> Id { get; }

    public HydrationProperties Dehydrate()
    {
        return HydrationProperties.FromDto(this);
    }
}