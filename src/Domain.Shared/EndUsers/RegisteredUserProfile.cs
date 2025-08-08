using Common;

namespace Domain.Shared.EndUsers;

public class RegisteredUserProfile
{
    public required string CountryCode { get; set; }

    public required string FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Locale { get; set; } = Locales.Default.ToString(); // for backwards compatibility

    public required string Timezone { get; set; }
}