using Domain.Interfaces.Entities;

namespace Domain.Common.UnitTests.Entities;

public class TestEvent : IDomainEvent
{
    public string? APropertyValue { get; set; }

    public DateTime OccurredUtc { get; set; }
#pragma warning disable CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
    public string? RootId { get; set; }
#pragma warning restore CS8766 // Nullability of reference types in return type doesn't match implicitly implemented member (possibly because of nullability attributes).
}