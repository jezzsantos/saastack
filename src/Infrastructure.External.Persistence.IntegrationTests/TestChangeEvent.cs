using Domain.Interfaces.Entities;

namespace Infrastructure.External.Persistence.IntegrationTests;

public class TestChangeEvent : IDomainEvent
{
    public required DateTime OccurredUtc { get; set; }

    public required string RootId { get; set; }
}