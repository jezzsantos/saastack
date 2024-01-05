using Application.Common;
using Application.Interfaces;
using Application.Resources.Shared;
using Common;
using Common.Extensions;
using Domain.Common.Identity;
using Domain.Common.ValueObjects;
using Domain.Shared;
using EndUsersApplication.Persistence;
using EndUsersDomain;
using PersonName = Application.Resources.Shared.PersonName;

namespace EndUsersApplication;

public class EndUsersApplication : IEndUsersApplication
{
    private readonly IIdentifierFactory _idFactory;
    private readonly IRecorder _recorder;
    private readonly IEndUserRepository _repository;

    public EndUsersApplication(IRecorder recorder, IIdentifierFactory idFactory, IEndUserRepository repository)
    {
        _recorder = recorder;
        _idFactory = idFactory;
        _repository = repository;
    }

    public async Task<Result<EndUser, Error>> GetPersonAsync(ICallerContext context, string id,
        CancellationToken cancellationToken)
    {
        var retrieved = await _repository.LoadAsync(id.ToId(), cancellationToken);
        if (!retrieved.IsSuccessful)
        {
            return retrieved.Error;
        }

        var user = retrieved.Value;

        _recorder.TraceInformation(context.ToCall(), "Retrieved user: {Id}", user.Id);

        return user.ToUser();
    }

    public async Task<Result<RegisteredEndUser, Error>> RegisterPersonAsync(ICallerContext context, string emailAddress,
        string firstName, string lastName, string? timezone, string? countryCode, bool termsAndConditionsAccepted,
        CancellationToken cancellationToken)
    {
        var created = EndUserRoot.Create(_recorder, _idFactory, UserClassification.Person);
        if (!created.IsSuccessful)
        {
            return created.Error;
        }

        var user = created.Value;

        //TODO: get/create the profile for this user (with email, first,last,tz,cc) - it may already be existent or not,
        // if existent, then someone attempted to re-register with the same email address! we need to send a courtesy notification to them.
        // if not, it needs to be created

        var username = EmailAddress.Create(emailAddress);
        if (!username.IsSuccessful)
        {
            return username.Error;
        }

        var roles = Roles.Create().Value;
        var featureLevels = FeatureLevels.Create().Value;
        user.Register(UserClassification.Person, roles, featureLevels, username.Value);

        var saved = await _repository.SaveAsync(user, cancellationToken);
        if (!saved.IsSuccessful)
        {
            return saved.Error;
        }

        _recorder.TraceInformation(context.ToCall(), "Registered user: {Id}", user.Id);

        return user.ToRegisteredUser(emailAddress, firstName, lastName, Timezones.FindOrDefault(timezone),
            CountryCodes.FindOrDefault(countryCode));
    }
}

internal static class EndUserConversionExtensions
{
    public static RegisteredEndUser ToRegisteredUser(this EndUserRoot user, string emailAddress, string firstName,
        string lastName, TimezoneIANA timezone, CountryCodeIso3166 countryCode)
    {
        var endUser = ToUser(user);
        var registeredUser = endUser.Convert<EndUser, RegisteredEndUser>();
        registeredUser.Profile = new DefaultMembershipProfile
        {
            Address = new ProfileAddress
            {
                CountryCode = countryCode.ToString()
            },
            AvatarUrl = null,
            DisplayName = firstName,
            EmailAddress = emailAddress,
            Name = new PersonName { FirstName = firstName, LastName = lastName },
            PhoneNumber = null,
            Timezone = timezone.ToString(),
            Id = user.Id,
            DefaultOrganisationId = null
        };

        return registeredUser;
    }

    public static EndUser ToUser(this EndUserRoot user)
    {
        return new EndUser
        {
            Id = user.Id,
            Access = user.Access.ToEnumOrDefault(EndUserAccess.Enabled),
            Status = user.Status.ToEnumOrDefault(EndUserStatus.Unregistered),
            Classification = user.Classification.ToEnumOrDefault(EndUserClassification.Person),
            FeatureLevels = user.Features.ToList(),
            Roles = user.Roles.ToList()
        };
    }
}