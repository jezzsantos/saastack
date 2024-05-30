using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class PasswordCredential : IIdentifiableResource
{
    public required EndUser User { get; set; }

    public required string Id { get; set; }
}

public class PasswordCredentialConfirmation
{
    public required string Token { get; set; }

    public required string Url { get; set; }
}