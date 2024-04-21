using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.ValueObjects;
using Domain.Events.Shared.EndUsers;
using Domain.Shared;
using UserProfilesDomain;
using PersonName = Domain.Shared.PersonName;

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

    public async Task<Result<Error>> HandleEndUserDefaultMembershipChangedAsync(ICallerContext caller,
        DefaultMembershipChanged domainEvent,
        CancellationToken cancellationToken)
    {
        var profile = await UpdateDefaultOrganizationAsync(caller, domainEvent.RootId, domainEvent.ToOrganizationId,
            cancellationToken);
        if (!profile.IsSuccessful)
        {
            return profile.Error;
        }

        return Result.Ok;
    }

    private async Task<Result<UserProfile, Error>> CreateProfileAsync(ICallerContext caller,
        UserProfileClassification classification, string userId, string? emailAddress, string firstName,
        string? lastName, string? timezone, string? countryCode,
        CancellationToken cancellationToken)
    {
        if (classification == UserProfileClassification.Person && emailAddress.HasNoValue())
        {
            return Error.RuleViolation(Resources.UserProfilesApplication_PersonMustHaveEmailAddress);
        }

        var retrievedById = await _repository.FindByUserIdAsync(userId.ToId(), cancellationToken);
        if (!retrievedById.IsSuccessful)
        {
            return retrievedById.Error;
        }

        if (retrievedById.Value.HasValue)
        {
            return Error.EntityExists(Resources.UserProfilesApplication_ProfileExistsForUser);
        }

        if (classification == UserProfileClassification.Person && emailAddress.HasValue())
        {
            var email = EmailAddress.Create(emailAddress);
            if (!email.IsSuccessful)
            {
                return email.Error;
            }

            var retrievedByEmail = await _repository.FindByEmailAddressAsync(email.Value, cancellationToken);
            if (!retrievedByEmail.IsSuccessful)
            {
                return retrievedByEmail.Error;
            }

            if (retrievedByEmail.Value.HasValue)
            {
                return Error.EntityExists(Resources.UserProfilesApplication_ProfileExistsForEmailAddress);
            }
        }

        var name = PersonName.Create(firstName, classification == UserProfileClassification.Person
            ? lastName
            : Optional<string>.None);
        if (!name.IsSuccessful)
        {
            return name.Error;
        }

        var created = UserProfileRoot.Create(_recorder, _identifierFactory,
            classification.ToEnumOrDefault(ProfileType.Person),
            userId.ToId(), name.Value);
        if (!created.IsSuccessful)
        {
            return created.Error;
        }

        var profile = created.Value;
        if (classification == UserProfileClassification.Person)
        {
            var personEmail = EmailAddress.Create(emailAddress!);
            if (!personEmail.IsSuccessful)
            {
                return personEmail.Error;
            }

            var emailed = profile.SetEmailAddress(userId.ToId(), personEmail.Value);
            if (!emailed.IsSuccessful)
            {
                return emailed.Error;
            }
        }

        var address = Address.Create(CountryCodes.FindOrDefault(countryCode));
        if (!address.IsSuccessful)
        {
            return address.Error;
        }

        var contacted = profile.SetContactAddress(userId.ToId(), address.Value);
        if (!contacted.IsSuccessful)
        {
            return contacted.Error;
        }

        var tz = Timezone.Create(Timezones.FindOrDefault(timezone));
        if (!tz.IsSuccessful)
        {
            return tz.Error;
        }

        var timezoned = profile.SetTimezone(userId.ToId(), tz.Value);
        if (!timezoned.IsSuccessful)
        {
            return timezoned.Error;
        }

        if (classification == UserProfileClassification.Person)
        {
            //Attempt to download the default avatar for the user. If this fails, we just move on
            var defaultAvatared = await _avatarService.FindAvatarAsync(caller, emailAddress!, cancellationToken);
            if (defaultAvatared is { IsSuccessful: true, Value.HasValue: true })
            {
                var upload = defaultAvatared.Value.Value;
                var avatared =
                    await ChangeAvatarInternalAsync(caller, userId.ToId(), profile, upload, cancellationToken);
                if (!avatared.IsSuccessful)
                {
                    return avatared.Error;
                }
            }
        }

        var saved = await _repository.SaveAsync(profile, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        profile = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Profile {Id} was created for user {UserId}", profile.Id,
            profile.UserId);

        return profile.ToProfile();
    }

    private async Task<Result<UserProfile, Error>> UpdateDefaultOrganizationAsync(ICallerContext caller, string userId,
        string defaultOrganizationId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindByUserIdAsync(userId.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var profile = retrieved.Value.Value;
        var defaulted = profile.ChangeDefaultOrganization(caller.ToCallerId(), defaultOrganizationId.ToId());
        if (!defaulted.IsSuccessful)
        {
            return defaulted.Error;
        }

        var saved = await _repository.SaveAsync(profile, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        profile = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Profile {Id} updated its default organization for user {UserId}",
            profile.Id,
            profile.UserId);

        return profile.ToProfile();
    }
}