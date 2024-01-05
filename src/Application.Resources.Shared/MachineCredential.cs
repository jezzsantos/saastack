using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class MachineCredential : IIdentifiableResource
{
    public required string ApiKey { get; set; }

    public required string CreatorId { get; set; }

    public required string Id { get; set; }
}