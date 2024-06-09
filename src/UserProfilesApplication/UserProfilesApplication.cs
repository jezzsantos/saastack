using Application.Common.Extensions;
using Application.Interfaces;
using Application.Resources.Shared;
using Application.Services.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Interfaces;
using Domain.Interfaces.Extensions;
using Domain.Shared;
using UserProfilesApplication.Persistence;
using UserProfilesDomain;
using PersonName = Domain.Shared.PersonName;

namespace UserProfilesApplication;

public partial class UserProfilesApplication : IUserProfilesApplication
{
    private readonly IAvatarService _avatarService;
    private readonly IIdentifierFactory _identifierFactory;
    private readonly IImagesService _imagesService;
    private readonly IRecorder _recorder;
    private readonly IUserProfileRepository _repository;

    public UserProfilesApplication(IRecorder recorder, IIdentifierFactory identifierFactory,
        IImagesService imagesService, IAvatarService avatarService, IUserProfileRepository repository)
    {
        _recorder = recorder;
        _identifierFactory = identifierFactory;
        _imagesService = imagesService;
        _avatarService = avatarService;
        _repository = repository;
    }

    public async Task<Result<UserProfile, Error>> ChangeProfileAvatarAsync(ICallerContext caller, string userId,
        FileUpload upload, CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindByUserIdAsync(userId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var profile = retrieved.Value.Value;
        var avatared = await ChangeAvatarInternalAsync(caller, caller.ToCallerId(), profile, upload, cancellationToken);
        if (avatared.IsFailure)
        {
            return avatared.Error;
        }

        var saved = await _repository.SaveAsync(profile, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        profile = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Profile {Id} avatar was added for user {UserId}", profile.Id,
            userId);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
            profile.ToUsageEvent(caller));

        return profile.ToProfile();
    }

    public async Task<Result<UserProfile, Error>> DeleteProfileAvatarAsync(ICallerContext caller, string userId,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindByUserIdAsync(userId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var profile = retrieved.Value.Value;
        var deleted = await profile.RemoveAvatarAsync(caller.ToCallerId(), async avatarId =>
        {
            var removed = await _imagesService.DeleteImageAsync(caller, avatarId, cancellationToken);
            return removed.IsFailure
                ? removed.Error
                : Result.Ok;
        });
        if (deleted.IsFailure)
        {
            return deleted.Error;
        }

        var saved = await _repository.SaveAsync(profile, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        profile = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Profile {Id} avatar was deleted for user {UserId}", profile.Id,
            userId);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
            profile.ToUsageEvent(caller));

        return profile.ToProfile();
    }

    public async Task<Result<Optional<UserProfile>, Error>> FindPersonByEmailAddressAsync(ICallerContext caller,
        string emailAddress, CancellationToken cancellationToken)
    {
        var email = EmailAddress.Create(emailAddress);
        if (email.IsFailure)
        {
            return email.Error;
        }

        var retrieved = await _repository.FindByEmailAddressAsync(email.Value, cancellationToken);
        if (retrieved.IsFailure)
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

    public async Task<Result<UserProfileForCaller, Error>> GetCurrentUserProfileAsync(ICallerContext caller,
        CancellationToken cancellationToken)
    {
        if (!caller.IsAuthenticated)
        {
            return new UserProfileForCaller
            {
                Address = new ProfileAddress
                {
                    CountryCode = CountryCodes.Default.ToString()
                },
                AvatarUrl = null,
                DisplayName = CallerConstants.AnonymousUserId,
                EmailAddress = null,
                Name = new Application.Resources.Shared.PersonName
                {
                    FirstName = CallerConstants.AnonymousUserId
                },
                PhoneNumber = null,
                Timezone = null,
                Classification = UserProfileClassification.Person,
                UserId = CallerConstants.AnonymousUserId,
                Id = CallerConstants.AnonymousUserId,
                DefaultOrganizationId = null,
                IsAuthenticated = false,
                Features = [],
                Roles = []
            };
        }

        var retrieved = await _repository.FindByUserIdAsync(caller.ToCallerId(), cancellationToken);
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var profile = retrieved.Value.Value;
        _recorder.TraceInformation(caller.ToCall(), "Profile {Id} was retrieved for current user {UserId}", profile.Id,
            profile.UserId);

        return profile.ToCurrentProfile(caller);
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
        if (retrieved.IsFailure)
        {
            return retrieved.Error;
        }

        if (!retrieved.Value.HasValue)
        {
            return Error.EntityNotFound();
        }

        var profile = retrieved.Value.Value;
        _recorder.TraceInformation(caller.ToCall(), "Profile {Id} was retrieved for user {UserId}", profile.Id,
            profile.UserId);

        return profile.ToProfile();
    }

    public async Task<Result<UserProfile, Error>> ChangeProfileAsync(ICallerContext caller, string userId,
        string? firstName, string? lastName, string? displayName, string? phoneNumber, string? timezone,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.FindByUserIdAsync(userId.ToId(), cancellationToken);
        if (retrieved.IsFailure)
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
            if (name.IsFailure)
            {
                return name.Error;
            }

            var named = profile.ChangeName(caller.ToCallerId(), name.Value);
            if (named.IsFailure)
            {
                return named.Error;
            }
        }

        if (displayName.HasValue())
        {
            var display = PersonDisplayName.Create(displayName);
            if (display.IsFailure)
            {
                return display.Error;
            }

            var displayed = profile.ChangeDisplayName(caller.ToCallerId(), display.Value);
            if (displayed.IsFailure)
            {
                return displayed.Error;
            }
        }

        if (phoneNumber.HasValue())
        {
            var phone = PhoneNumber.Create(phoneNumber);
            if (phone.IsFailure)
            {
                return phone.Error;
            }

            var phoned = profile.ChangePhoneNumber(caller.ToCallerId(), phone.Value);
            if (phoned.IsFailure)
            {
                return phoned.Error;
            }
        }

        if (timezone.HasValue())
        {
            var tz = Timezone.Create(Timezones.FindOrDefault(timezone));
            if (tz.IsFailure)
            {
                return tz.Error;
            }

            var timezoned = profile.SetTimezone(caller.ToCallerId(), tz.Value);
            if (timezoned.IsFailure)
            {
                return timezoned.Error;
            }
        }

        var saved = await _repository.SaveAsync(profile, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        profile = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Profile {Id} was updated for user {UserId}", profile.Id,
            profile.UserId);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
            profile.ToUsageEvent(caller));

        return profile.ToProfile();
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
        if (retrieved.IsFailure)
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
        if (address.IsFailure)
        {
            return address.Error;
        }

        var contacted = profile.SetContactAddress(caller.ToCallerId(), address.Value);
        if (contacted.IsFailure)
        {
            return contacted.Error;
        }

        var saved = await _repository.SaveAsync(profile, cancellationToken);
        if (saved.IsFailure)
        {
            return saved.Error;
        }

        profile = saved.Value;
        _recorder.TraceInformation(caller.ToCall(), "Profile {Id} contact address was updated for user {UserId}",
            profile.Id, userId);
        _recorder.TrackUsage(caller.ToCall(), UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged,
            profile.ToUsageEvent(caller));

        return profile.ToProfile();
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
        if (retrieved.IsFailure)
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

    private async Task<Result<Error>> ChangeAvatarInternalAsync(ICallerContext caller, Identifier modifierId,
        UserProfileRoot profile, FileUpload upload, CancellationToken cancellationToken)
    {
        return await profile.ChangeAvatarAsync(modifierId, async displayName =>
        {
            var created = await _imagesService.CreateImageAsync(caller, upload, displayName.Text, cancellationToken);
            if (created.IsFailure)
            {
                return created.Error;
            }

            return Avatar.Create(created.Value.Id.ToId(), created.Value.Url);
        }, async avatarId =>
        {
            var removed = await _imagesService.DeleteImageAsync(caller, avatarId, cancellationToken);
            return removed.IsFailure
                ? removed.Error
                : Result.Ok;
        });
    }
}

internal static class UserProfileConversionExtensions
{
    public static UserProfileForCaller ToCurrentProfile(this UserProfileRoot profile, ICallerContext caller)
    {
        var dto = profile.ToProfile().Convert<UserProfile, UserProfileForCaller>();
        dto.IsAuthenticated = caller.IsAuthenticated;
        dto.Roles = caller.Roles.Platform.Denormalize().ToList();
        dto.Features = caller.Features.Platform.Denormalize().ToList();
        dto.DefaultOrganizationId = profile.DefaultOrganizationId.ValueOrDefault!;

        return dto;
    }

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
            AvatarUrl = profile.Avatar.HasValue
                ? profile.Avatar.Value.Url
                : null
        };
    }

    public static Dictionary<string, object> ToUsageEvent(this UserProfileRoot profile, ICallerContext caller)
    {
        var context = new Dictionary<string, object>
        {
            [UsageConstants.Properties.UserIdOverride] = profile.UserId,
            [UsageConstants.Properties.Name] = profile.Name.Value.FullName,
            [UsageConstants.Properties.Timezone] = profile.Timezone.Code.ToString()
        };
        if (profile.EmailAddress.HasValue)
        {
            context[UsageConstants.Properties.EmailAddress] = profile.EmailAddress.Value.Address;
        }

        if (profile.Avatar.HasValue)
        {
            context[UsageConstants.Properties.AvatarUrl] = profile.Avatar.Value.Url;
        }

        return context;
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