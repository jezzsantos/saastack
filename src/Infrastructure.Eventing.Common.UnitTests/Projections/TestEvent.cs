using Domain.Interfaces.Entities;

namespace Infrastructure.Eventing.Common.UnitTests.Projections;

public class TestEvent : IDomainEvent
{
    public DateTime OccurredUtc { get; set; }

    public string RootId { get; set; } = "arootid";
}