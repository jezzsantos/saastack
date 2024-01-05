using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class MachineCredential : IIdentifiableResource
{
    public required string ApiKey { get; set; }

    public required string CreatedById { get; set; }

    public string? Description { get; set; }

    public DateTime? ExpiresOnUtc { get; set; }

    public required string Id { get; set; }
}