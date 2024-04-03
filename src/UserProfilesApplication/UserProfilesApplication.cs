using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Shared;
using UserProfilesApplication.Persistence;
using UserProfilesDomain;
using PersonName = Domain.Shared.PersonName;

namespace UserProfilesApplication;

public partial class UserProfilesApplication : IUserProfilesApplication
{
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IRecorder _recorder;
    private readonly IUserProfileRepository _repository;

    public UserProfilesApplication(IRecorder recorder, IIdentifierFactory identifierFactory,
        IUserProfileRepository repository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _repository = repository;
    }

    public async Task<Result<Optional<UserProfile>, Error>> FindPersonByEmailAddressAsync(ICallerContext caller,
        string emailAddress, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(emailAddress);
        if (!email.IsSuccessful)
        {
            return email.Error;
        }

        var retrieved = await _repository.FindByEmailAddressAsync(email.Value, cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var profile = retrieved.Value;
        if (retrieved.Value.HasValue)
        {
            return profile.Value.ToProfile().ToOptional();
        }

        return Optional<UserProfile>.None;
    }

    public async Task<Result<UserProfile, Error>> GetProfileAsync(ICallerContext caller, string userId,
        CancellationToken cancellationToken)
    {
        if (!caller.IsServiceAccount
            && userId != caller.CallerId)
        {
            return Error.ForbiddenAccess();
        }

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

        _recorder.TraceInformation(caller.ToCall(), "Profile {Id} was retrieved for user {userId}", profile.Id, userId);
        return profile.ToProfile();
    }

    public async Task<Result<UserProfile, Error>> ChangeProfileAsync(ICallerContext caller, string userId,
        string? firstName, string? lastName, string? displayName, string? phoneNumber, string? timezone,
        CancellationToken cancellationToken)
    {
        if (userId != caller.CallerId)
        {
            return Error.ForbiddenAccess();
        }

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
        if (firstName.HasValue() || lastName.HasValue())
        {
            var backupFirstname = profile.Name.Value.FirstName;
            var backupLastname = profile.Name.Value.LastName.ValueOrDefault?.Text;
            var name = PersonName.Create(firstName ?? backupFirstname, lastName ?? backupLastname);
            if (!name.IsSuccessful)
            {
                return name.Error;
            }

            var named = profile.ChangeName(caller.ToCallerId(), name.Value);
            if (!named.IsSuccessful)
            {
                return named.Error;
            }
        }

        if (displayName.HasValue())
        {
            var display = PersonDisplayName.Create(displayName);
            if (!display.IsSuccessful)
            {
                return display.Error;
            }

            var displayed = profile.ChangeDisplayName(caller.ToCallerId(), display.Value);
            if (!displayed.IsSuccessful)
            {
                return displayed.Error;
            }
        }

        if (phoneNumber.HasValue())
        {
            var phone = PhoneNumber.Create(phoneNumber);
            if (!phone.IsSuccessful)
            {
                return phone.Error;
            }

            var phoned = profile.ChangePhoneNumber(caller.ToCallerId(), phone.Value);
            if (!phoned.IsSuccessful)
            {
                return phoned.Error;
            }
        }

        if (timezone.HasValue())
        {
            var tz = Timezone.Create(Timezones.FindOrDefault(timezone));
            if (!tz.IsSuccessful)
            {
                return tz.Error;
            }

            var timezoned = profile.SetTimezone(caller.ToCallerId(), tz.Value);
            if (!timezoned.IsSuccessful)
            {
                return timezoned.Error;
            }
        }

        var saved = await _repository.SaveAsync(profile, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Profile {Id} was updated for user {userId}", profile.Id, userId);

        return saved.Value.ToProfile();
    }

    public async Task<Result<UserProfile, Error>> ChangeContactAddressAsync(ICallerContext caller, string userId,
        string? line1, string? line2,
        string? line3, string? city, string? state, string? countryCode, string? zipCode,
        CancellationToken cancellationToken)
    {
        if (userId != caller.CallerId)
        {
            return Error.ForbiddenAccess();
        }

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

        var address = Address.Create(
            line1 ?? profile.Address.Line1,
            line2 ?? profile.Address.Line2,
            line3 ?? profile.Address.Line3,
            city ?? profile.Address.City,
            state ?? profile.Address.State,
            CountryCodes.FindOrDefault(countryCode ?? profile.Address.CountryCode.ToString()),
            zipCode ?? profile.Address.Zip);
        if (!address.IsSuccessful)
        {
            return address.Error;
        }

        var contacted = profile.SetContactAddress(caller.ToCallerId(), address.Value);
        if (!contacted.IsSuccessful)
        {
            return contacted.Error;
        }

        var saved = await _repository.SaveAsync(profile, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Profile {Id} contact address was updated for user {userId}",
            profile.Id, userId);

        return saved.Value.ToProfile();
    }

    public async Task<Result<List<UserProfile>, Error>> GetAllProfilesAsync(ICallerContext caller, List<string> ids,
        GetOptions options, CancellationToken cancellationToken)
    {
        if (ids.HasNone())
        {
            return new List<UserProfile>();
        }

        var retrieved =
            await _repository.SearchAllByUserIdsAsync(ids.Select(id => id.ToId()).ToList(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var profiles = retrieved.Value;
        if (profiles.HasNone())
        {
            return new List<UserProfile>();
        }

        _recorder.TraceInformation(caller.ToCall(),
            "Profiles were retrieved for {ExpectedCount} users, and returned {ActualCount} profiles", ids.Count,
            profiles.Count);
        return profiles
            .ConvertAll(profile => profile.ToProfile())
            .ToList();
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
            var email2 = EmailAddress.Create(emailAddress!);
            if (!email2.IsSuccessful)
            {
                return email2.Error;
            }

            var emailed = profile.SetEmailAddress(userId.ToId(), email2.Value);
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

        var saved = await _repository.SaveAsync(profile, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Profile {Id} was created for user {userId}", profile.Id, userId);

        return saved.Value.ToProfile();
    }
}

internal static class UserProfileConversionExtensions
{
    public static UserProfile ToProfile(this UserProfileRoot profile)
    {
        return new UserProfile
        {
            Id = profile.Id,
            Classification = profile.Type.ToEnumOrDefault(UserProfileClassification.Person),
            UserId = profile.UserId,
            Name = profile.Name.ToName(),
            DisplayName = profile.DisplayName.ValueOrDefault!,
            EmailAddress = profile.EmailAddress.ValueOrDefault?.Address,
            PhoneNumber = profile.PhoneNumber.ValueOrDefault!,
            Address = profile.Address.ToAddress(),
            Timezone = profile.Timezone.Code.ToString(),
            AvatarUrl = profile.AvatarUrl.ValueOrDefault
        };
    }

    private static ProfileAddress ToAddress(this Address address)
    {
        var dto = address.Convert<Address, ProfileAddress>();
        dto.Line1 = address.Line1;
        dto.Line2 = address.Line2;
        dto.Line3 = address.Line3;
        dto.City = address.City;
        dto.State = address.State;
        dto.CountryCode = address.CountryCode.Alpha3;
        dto.Zip = address.Zip;

        return dto;
    }

    private static Application.Resources.Shared.PersonName ToName(this Optional<PersonName> name)
    {
        return name.HasValue
            ? new Application.Resources.Shared.PersonName
            {
                FirstName = name.Value.FirstName,
                LastName = name.Value.LastName.ValueOrDefault!
            }
            : new Application.Resources.Shared.PersonName
            {
                FirstName = string.Empty
            };
    }
}