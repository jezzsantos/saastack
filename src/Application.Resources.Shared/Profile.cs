using Application.Interfaces.Resources;

namespace Application.Resources.Shared;

public class Profile : IIdentifiableResource
{
    public ProfileAddress? Address { get; set; }

    public string? AvatarUrl { get; set; }

    public required string DisplayName { get; set; }

    public string? EmailAddress { get; set; }

    public required PersonName Name { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Timezone { get; set; }

    public required string Id { get; set; }
}

public class PersonName
{
    public required string FirstName { get; set; }

    public string? LastName { get; set; }
}

public class ProfileAddress
{
    public string? City { get; set; }

    public required string CountryCode { get; set; }

    public string? Line1 { get; set; }

    public string? Line2 { get; set; }

    public string? Line3 { get; set; }

    public string? State { get; set; }

    public string? Zip { get; set; }
}