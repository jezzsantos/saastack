using Domain.Interfaces.Entities;

namespace Infrastructure.Hosting.Common.UnitTests.ApplicationServices.Eventing;

public class TestEvent : IDomainEvent
{
    public required string Id { get; set; }

    public DateTime OccurredUtc { get; set; }

    public string RootId { get; set; } = "arootid";
}