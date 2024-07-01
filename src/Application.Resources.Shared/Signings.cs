using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class SigningRequest : IIdentifiableResource
{
    public required string Id { get; set; }

    public required string OrganizationId { get; set; }
}

public class Signee
{
    public string? EmailAddress { get; set; }

    public string? PhoneNumber { get; set; }
}