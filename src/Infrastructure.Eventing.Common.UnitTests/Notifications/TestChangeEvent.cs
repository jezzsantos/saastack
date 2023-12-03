using Domain.Interfaces.Entities;

namespace Infrastructure.Eventing.Common.UnitTests.Notifications;

public class TestChangeEvent : IDomainEvent
{
    public DateTime OccurredUtc { get; set; }

    public string RootId { get; set; } = "arootid";
}