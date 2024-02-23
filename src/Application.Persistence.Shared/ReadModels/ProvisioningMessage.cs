using Application.Interfaces;
using Application.Interfaces.Services;
using QueryAny;

namespace Application.Persistence.Shared.ReadModels;

[EntityName(WorkerConstants.Queues.Provisionings)]
public class ProvisioningMessage : QueuedMessage
{
    public Dictionary<string, TenantSetting> Settings { get; set; } = new();
}