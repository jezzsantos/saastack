using Application.Interfaces.Resources;
using Common;

namespace Application.Resources.Shared;

public class UserProfile : IIdentifiableResource
{
    public ProfileAddress Address { get; set; } = new() { CountryCode = CountryCodes.Default.ToString() };

    public string? AvatarUrl { get; set; }

    public required string DisplayName { get; set; }

    public string? EmailAddress { get; set; }

    public required PersonName Name { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Timezone { get; set; }

    public UserProfileClassification Classification { get; set; }

    public required string UserId { get; set; }

    public required string Id { get; set; }
}

public enum UserProfileClassification
{
    Person = 0,
    Machine = 1
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

public class UserProfileWithDefaultMembership : UserProfile
{
    public string? DefaultOrganizationId { get; set; }
}