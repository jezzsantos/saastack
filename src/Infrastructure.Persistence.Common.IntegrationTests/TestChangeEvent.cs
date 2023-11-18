using Domain.Interfaces.Entities;

namespace Infrastructure.Persistence.Common.IntegrationTests;

public class TestChangeEvent : IDomainEvent
{
    public DateTime OccurredUtc { get; set; }

    public required string RootId { get; set; }
}