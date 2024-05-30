using Application.Persistence.Common;
using Common;
using Domain.Common.ValueObjects;
using QueryAny;

namespace EventNotificationsApplication.Persistence.ReadModels;

[EntityName("DomainEvent")]
public class DomainEvent : ReadModelEntity
{
    public Optional<string> Data { get; set; }

    public Optional<string> EventType { get; set; }

    public Optional<EventMetadata> Metadata { get; set; }

    public Optional<string> RootAggregateType { get; set; }

    public Optional<string> StreamName { get; set; }

    public Optional<int> Version { get; set; }
}