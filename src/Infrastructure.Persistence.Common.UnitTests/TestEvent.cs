using Domain.Interfaces.Entities;

namespace Infrastructure.Persistence.Common.UnitTests;

public class TestEvent : IDomainEvent
{
    public DateTime OccurredUtc { get; set; }

    public string RootId { get; set; } = null!;
}