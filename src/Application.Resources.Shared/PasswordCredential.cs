using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class PasswordCredential : IIdentifiableResource
{
    public required RegisteredEndUser User { get; set; }

    public required string Id { get; set; }
}