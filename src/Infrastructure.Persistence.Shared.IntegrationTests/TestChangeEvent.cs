using Domain.Interfaces.Entities;

namespace Infrastructure.Persistence.Shared.IntegrationTests;

public class TestChangeEvent : IDomainEvent
{
    public required DateTime OccurredUtc { get; set; }

    public required string RootId { get; set; }
}