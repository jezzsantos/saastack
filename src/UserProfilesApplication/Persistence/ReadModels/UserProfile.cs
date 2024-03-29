using Application.Persistence.Common;
using Common;
using QueryAny;

namespace UserProfilesApplication.Persistence.ReadModels;

[EntityName("UserProfile")]
public class UserProfile : ReadModelEntity
{
    public Optional<string> AvatarId { get; set; }

    public Optional<string> CountryCode { get; set; }

    public Optional<string> DisplayName { get; set; }

    public Optional<string> EmailAddress { get; set; }

    public Optional<string> FirstName { get; set; }

    public Optional<string> LastName { get; set; }

    public Optional<string> PhoneNumber { get; set; }

    public Optional<string> Timezone { get; set; }

    public Optional<string> Type { get; set; }

    public Optional<string> UserId { get; set; }
}