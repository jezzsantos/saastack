using Application.Common.Extensions;
using Application.Interfaces;
using Application.Persistence.Shared;
using Common;
using Common.Configuration;
using Common.Extensions;

namespace Infrastructure.External.ApplicationServices;

/// <summary>
///     Provides an adapter to the UserPilot.com service
///     <see href="https://docs.userpilot.com/article/195-identify-users-and-track-api" />
///     In UserPilot, a user is assumed to be unique across all companies, which means that a unique user belongs to a
///     unique company. A unique user cannot belong to two different companies at the same time (also they cannot be
///     removed from a company).
///     Thus, when we identify a user, we need to use their userId@tenantId as their unique identifier.
///     Certain events will require us to send two usage events, one for the platform user and one for the tenant user.
///     * <see cref="UsageConstants.Events.UsageScenarios.Generic.UserLogin" /> AND
///     * <see cref="UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged" />
/// </summary>
public sealed class UserPilotHttpServiceClient : IUsageDeliveryService
{
    internal const string CompanyIdPropertyName = "id";
    internal const string CompanyNamePropertyName = "name";
    internal const string CreatedAtPropertyName = "created_at";
    internal const string UserEmailAddressPropertyName = "email";
    internal const string UserNamePropertyName = "name";

    private readonly IRecorder _recorder;
    private readonly IUserPilotClient _serviceClient;
    private readonly IUsageDeliveryTranslator _translator;

    public UserPilotHttpServiceClient(IRecorder recorder, IConfigurationSettings settings,
        IHttpClientFactory httpClientFactory) : this(recorder,
        new UserPilotClient(recorder, settings, httpClientFactory))
    {
    }

    internal UserPilotHttpServiceClient(IRecorder recorder, IUserPilotClient serviceClient)
    {
        _recorder = recorder;
        _serviceClient = serviceClient;
        _translator = new UsageDeliveryTranslator();
    }

    public async Task<Result<Error>> DeliverAsync(ICallerContext caller, string forId, string eventName,
        Dictionary<string, string>? additional = null, CancellationToken cancellationToken = default)
    {
        _translator.StartTranslation(caller, forId, eventName, additional);
        var userId = _translator.UserId;
        var isIdentifiableEvent = _translator.IsUserIdentifiableEvent();
        if (isIdentifiableEvent)
        {
            var identified = await IdentifyUserAsync(caller, userId, eventName, additional, cancellationToken);
            if (identified.IsFailure)
            {
                return identified.Error;
            }
        }

        var eventProperties = _translator.PrepareProperties(ConvertToUserPilotDataType);
        var tracked = await TrackEventAsync(caller, userId, eventName, eventProperties, cancellationToken);
        if (tracked.IsFailure)
        {
            return tracked.Error;
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.UserLogin
            or UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged)
        {
            if (!isIdentifiableEvent)
            {
                return Result.Ok;
            }

            if (additional.NotExists()
                || !additional.TryGetValue(UsageConstants.Properties.DefaultOrganizationId,
                    out var defaultOrganizationId))
            {
                return Result.Ok;
            }

            _translator.RecalculateTenantId(defaultOrganizationId);
            var secondUserId = _translator.UserId;
            var secondEventProperties = _translator.PrepareProperties(ConvertToUserPilotDataType);

            var identifiedTenant =
                await IdentifyUserAsync(caller, secondUserId, eventName, additional, cancellationToken);
            if (identifiedTenant.IsFailure)
            {
                return identifiedTenant.Error;
            }

            var secondTracked =
                await TrackEventAsync(caller, secondUserId, eventName, secondEventProperties, cancellationToken);
            if (secondTracked.IsFailure)
            {
                return secondTracked.Error;
            }
        }

        return Result.Ok;
    }

    private async Task<Result<Error>> IdentifyUserAsync(ICallerContext caller, string userId,
        string eventName, Dictionary<string, string>? additional, CancellationToken cancellationToken)
    {
        _recorder.TraceInformation(caller.ToCall(), "Identifying user in UserPilot for {User}", userId);

        var userMetadata = CreateIdentifiedUserProperties(eventName, additional);
        var companyMetadata = CreateIdentifiedCompanyProperties(eventName, additional);
        var identified =
            await _serviceClient.IdentifyUserAsync(caller.ToCall(), userId, userMetadata, companyMetadata,
                cancellationToken);
        if (identified.IsFailure)
        {
            return identified.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Identified user in UserPilot for {User} successfully",
            userId);

        return Result.Ok;
    }

    private async Task<Result<Error>> TrackEventAsync(ICallerContext caller, string userId,
        string eventName, Dictionary<string, string> metadata, CancellationToken cancellationToken)
    {
        _recorder.TraceInformation(caller.ToCall(), "Tracking event {Event} in UserPilot for {User}", eventName,
            userId);

        var tracked =
            await _serviceClient.TrackEventAsync(caller.ToCall(), userId, eventName, metadata, cancellationToken);
        if (tracked.IsFailure)
        {
            return tracked.Error;
        }

        _recorder.TraceInformation(caller.ToCall(), "Tracked event {Event} in UserPilot for {User} successfully",
            eventName, userId);

        return Result.Ok;
    }

    private static Dictionary<string, string> CreateIdentifiedUserProperties(string eventName,
        Dictionary<string, string>? additional)
    {
        var metadata = new Dictionary<string, string>();
        if (additional.NotExists())
        {
            return metadata;
        }

        if (eventName is UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated
            or UsageConstants.Events.UsageScenarios.Generic.MachineRegistered
            or UsageConstants.Events.UsageScenarios.Generic.MembershipAdded)
        {
            var now = DateTime.UtcNow.ToNearestSecond().ToIso8601();
            metadata.TryAdd(CreatedAtPropertyName, ConvertToUserPilotDataType(now));
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.UserLogin
            or UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated
            or UsageConstants.Events.UsageScenarios.Generic.MachineRegistered
            or UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged
            or UsageConstants.Events.UsageScenarios.Generic.MembershipChanged
           )
        {
            if (additional.TryGetValue(UsageConstants.Properties.EmailAddress, out var emailAddress))
            {
                metadata.TryAdd(UserEmailAddressPropertyName, ConvertToUserPilotDataType(emailAddress));
            }

            if (additional.TryGetValue(UsageConstants.Properties.Name, out var name))
            {
                metadata.TryAdd(UserNamePropertyName, ConvertToUserPilotDataType(name));
            }
        }

        return metadata;
    }

    private static Dictionary<string, string> CreateIdentifiedCompanyProperties(string eventName,
        Dictionary<string, string>? additional)
    {
        var metadata = new Dictionary<string, string>();
        if (additional.NotExists())
        {
            return metadata;
        }

        if (eventName is UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated)
        {
            var now = DateTime.UtcNow.ToNearestSecond().ToIso8601();
            metadata.TryAdd(CreatedAtPropertyName, ConvertToUserPilotDataType(now));
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated
            or UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged
           )
        {
            if (additional.TryGetValue(UsageConstants.Properties.Id, out var companyId))
            {
                metadata.TryAdd(CompanyIdPropertyName, ConvertToUserPilotDataType(companyId));
            }

            if (additional.TryGetValue(UsageConstants.Properties.Name, out var name))
            {
                metadata.TryAdd(CompanyNamePropertyName, ConvertToUserPilotDataType(name));
            }
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.MembershipAdded
           )
        {
            if (additional.TryGetValue(UsageConstants.Properties.TenantIdOverride, out var companyId))
            {
                metadata.TryAdd(CompanyIdPropertyName, ConvertToUserPilotDataType(companyId));
            }

            if (additional.TryGetValue(UsageConstants.Properties.Name, out var name))
            {
                metadata.TryAdd(CompanyNamePropertyName, ConvertToUserPilotDataType(name));
            }
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.MembershipChanged
           )
        {
            if (additional.TryGetValue(UsageConstants.Properties.TenantIdOverride, out var companyId))
            {
                metadata.TryAdd(CompanyIdPropertyName, ConvertToUserPilotDataType(companyId));
            }
        }

        return metadata;
    }

    /// <summary>
    ///     UserPilot only supports strings, where: datetime is in UnixSeconds
    /// </summary>
    private static string ConvertToUserPilotDataType(string value)
    {
        if (DateTime.TryParse(value, out var dateTime))
        {
            return dateTime.ToUnixSeconds().ToString();
        }

        return value;
    }
}