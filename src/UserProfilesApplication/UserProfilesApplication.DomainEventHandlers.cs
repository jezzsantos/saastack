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
        if (profile.IsFailure)
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
        if (profile.IsFailure)
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
        if (retrievedById.IsFailure)
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
            if (email.IsFailure)
            {
                return email.Error;
            }

            var retrievedByEmail = await _repository.FindByEmailAddressAsync(email.Value, cancellationToken);
            if (retrievedByEmail.IsFailure)
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
        if (name.IsFailure)
        {
            return name.Error;
        }

        var created = UserProfileRoot.Create(_recorder, _identifierFactory,
            classification.ToEnumOrDefault(ProfileType.Person),
            userId.ToId(), name.Value);
        if (created.IsFailure)
        {
            return created.Error;
        }

        var profile = created.Value;
        if (classification == UserProfileClassification.Person)
        {
            var personEmail = EmailAddress.Create(emailAddress!);
            if (personEmail.IsFailure)
            {
                return personEmail.Error;
            }

            var emailed = profile.SetEmailAddress(userId.ToId(), personEmail.Value);
            if (emailed.IsFailure)
            {
                return emailed.Error;
            }
        }

        var address = Address.Create(CountryCodes.FindOrDefault(countryCode));
        if (address.IsFailure)
        {
            return address.Error;
        }

        var contacted = profile.SetContactAddress(userId.ToId(), address.Value);
        if (contacted.IsFailure)
        {
            return contacted.Error;
        }

        var tz = Timezone.Create(Timezones.FindOrDefault(timezone));
        if (tz.IsFailure)
        {
            return tz.Error;
        }

        var timezoned = profile.SetTimezone(userId.ToId(), tz.Value);
        if (timezoned.IsFailure)
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
                if (avatared.IsFailure)
                {
                    return avatared.Error;
                }
            }
        }

        var saved = await _repository.SaveAsync(profile, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        profile = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Profile {Id} was created for user {UserId}", profile.Id,
            profile.UserId);

        return profile.ToProfile();
    }

    private async Task<Result<Optional<UserProfile>, Error>> UpdateDefaultOrganizationAsync(ICallerContext caller,
        string userId,
        string defaultOrganizationId, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindByUserIdAsync(userId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            // Might be a machine (or not exists)
            return Optional<UserProfile>.None;
        }

        var profile = retrieved.Value.Value;
        var defaulted = profile.ChangeDefaultOrganization(caller.ToCallerId(), defaultOrganizationId.ToId());
        if (defaulted.IsFailure)
        {
            return defaulted.Error;
        }

        var saved = await _repository.SaveAsync(profile, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        profile = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Profile {Id} updated its default organization for user {UserId}",
            profile.Id,
            profile.UserId);

        return profile.ToProfile().ToOptional();
    }
}