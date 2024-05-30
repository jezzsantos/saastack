using Application.Interfaces;
using Application.Persistence.Interfaces;
using QueryAny;

namespace Application.Persistence.Shared.ReadModels;

[EntityName(WorkerConstants.MessageBuses.Topics.DomainEvents)]
public class DomainEventingMessage : QueuedMessage
{
    public EventStreamChangeEvent? Event { get; set; }
}