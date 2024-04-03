using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Events.Shared.EndUsers;

namespace UserProfilesApplication;

partial class UserProfilesApplication
{
    public async Task<Result<Error>> HandleEndUserRegisteredAsync(ICallerContext caller, Registered domainEvent,
        CancellationToken cancellationToken)
    {
        var classification = domainEvent.Classification.ToEnumOrDefault(UserProfileClassification.Person);
        var profile =
            await CreateProfileAsync(caller, classification, domainEvent.RootId, domainEvent.Username,
                domainEvent.UserProfile.FirstName, domainEvent.UserProfile.LastName, domainEvent.UserProfile.Timezone,
                domainEvent.UserProfile.CountryCode, cancellationToken);
        if (!profile.IsSuccessful)
        {
            return profile.Error;
        }

        return Result.Ok;
    }
}