using Application.Interfaces;
using Common.Extensions;
using Domain.Interfaces;

namespace Infrastructure.External.ApplicationServices;

/// <summary>
///     Defines a translator that translates usage delivery events.
/// </summary>
public interface IUsageDeliveryTranslator
{
    public string UserId { get; }

    /// <summary>
    ///     Determines whether the specified event identifies the user, and if so returns their ID.
    /// </summary>
    bool IsUserIdentifiableEvent();

    /// <summary>
    ///     Prepares the properties of the event for delivery.
    ///     This method applies any additional properties, using the specified <see cref="converter" />.
    ///     This method removes any properties that are not relevant for the event.
    /// </summary>
    Dictionary<string, string> PrepareProperties(Func<string, string> converter);

    /// <summary>
    ///     Recalculates the tenant ID and user ID and overrides based on the specified <paramref name="tenantId" />.
    /// </summary>
    void RecalculateTenantId(string tenantId);

    /// <summary>
    ///     Begins the translation process for a usage delivery event.
    ///     Note: must be called before any other methods.
    /// </summary>
    void StartTranslation(ICallerContext caller, string forId, string eventName,
        Dictionary<string, string>? additional, bool createdTenantedUserIds);
}

/// <summary>
///     Provides a basic implementation of the <see cref="IUsageDeliveryTranslator" /> interface.
/// </summary>
public class UsageDeliveryTranslator : IUsageDeliveryTranslator
{
    private static readonly string[] IgnoredCustomEventProperties =
    [
        UsageConstants.Properties.UserIdOverride,
        UsageConstants.Properties.TenantIdOverride,
        UsageConstants.Properties.DefaultOrganizationId
    ];
    private UsageTranslationOptions? _options;

    /// <summary>
    ///     Determines whether the specified event identifies the user, and if so returns their ID.
    ///     Certain events will "identify" a user, and can be used to create or update the user's details:
    ///     * <see cref="UsageConstants.Events.UsageScenarios.Generic.UserLogin" />
    ///     - identify/create the platform-user and default-tenant-user with their email and name
    ///     * <see cref="UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged" />
    ///     - identify/create the platform-user and default-tenant-user and change the email and name of both
    ///     * <see cref="UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated" />
    ///     - identify/create the platform-user with their email and name
    ///     * <see cref="UsageConstants.Events.UsageScenarios.Generic.MachineRegistered" />
    ///     - identify/create the platform-user with their name
    ///     * <see cref="UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated" />
    ///     * - identify/create the tenant-user and create the company with its name
    ///     * <see cref="UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged" />
    ///     * - identify/create the platform-user and change their company's name
    ///     * <see cref="UsageConstants.Events.UsageScenarios.Generic.MembershipAdded" />
    ///     * - identify/create the tenant-user and change their company
    ///     * <see cref="UsageConstants.Events.UsageScenarios.Generic.MembershipChanged" />
    ///     * - identify/create the tenanted-user and change their email and name (of their "changed"
    ///     organization)
    /// </summary>
    public bool IsUserIdentifiableEvent()
    {
        if (_options.NotExists())
        {
            throw new InvalidOperationException(
                Resources.UsageDeliveryTranslator_NotStarted.Format(nameof(StartTranslation)));
        }

        if (_options.Additional.NotExists())
        {
            return false;
        }

        // Updates the user details
        if (_options.EventName
            is UsageConstants.Events.UsageScenarios.Generic.UserLogin)
        {
            return _options.Additional.TryGetValue(UsageConstants.Properties.UserIdOverride, out _);
        }

        // Updates the email or name of a user
        if (_options.EventName
            is UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated
            or UsageConstants.Events.UsageScenarios.Generic.MachineRegistered)
        {
            return _options.Additional.TryGetValue(UsageConstants.Properties.Id, out _);
        }

        if (_options.EventName
            is UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged)
        {
            return _options.Additional.TryGetValue(UsageConstants.Properties.Id, out _)
                   && (_options.Additional.TryGetValue(UsageConstants.Properties.Name, out _)
                       || _options.Additional.TryGetValue(UsageConstants.Properties.EmailAddress, out _));
        }

        // Updates the company details
        if (_options.EventName
            is UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated
            or UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged)
        {
            return _options.Additional.TryGetValue(UsageConstants.Properties.Id, out _);
        }

        // Updates the company details and user details
        if (_options.EventName
            is UsageConstants.Events.UsageScenarios.Generic.MembershipAdded
            or UsageConstants.Events.UsageScenarios.Generic.MembershipChanged)
        {
            return _options.Additional.TryGetValue(UsageConstants.Properties.Id, out _)
                   && _options.Additional.TryGetValue(UsageConstants.Properties.TenantIdOverride, out _);
        }

        return false;
    }

    public Dictionary<string, string> PrepareProperties(Func<string, string> converter)
    {
        if (_options.NotExists())
        {
            throw new InvalidOperationException(
                Resources.UsageDeliveryTranslator_NotStarted.Format(nameof(StartTranslation)));
        }

        var properties = new Dictionary<string, string>();

        var tenantId = _options.RecalculateTenantId(_options.Additional);
        properties.TryAdd(UsageConstants.Properties.TenantId, tenantId);

        if (_options.Additional.NotExists())
        {
            return properties;
        }

        if (_options.EventName
            is UsageConstants.Events.UsageScenarios.Generic.UserLogin
            or UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated
            or UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged
           )
        {
            if (_options.Additional.TryGetValue(UsageConstants.Properties.UserIdOverride, out var overriddenUserId))
            {
                properties.TryAdd(UsageConstants.Properties.Id, converter(overriddenUserId));
            }
        }

        foreach (var pair in _options.Additional.Where(
                     pair => IgnoredCustomEventProperties.NotContainsIgnoreCase(pair.Key)))
        {
            properties.TryAdd(pair.Key, converter(pair.Value));
        }

        return properties;
    }

    public void RecalculateTenantId(string tenantId)
    {
        if (_options.NotExists())
        {
            throw new InvalidOperationException(
                Resources.UsageDeliveryTranslator_NotStarted.Format(nameof(StartTranslation)));
        }

        _options.RecalculateTenantId(tenantId);
    }

    public void StartTranslation(ICallerContext caller, string forId, string eventName,
        Dictionary<string, string>? additional, bool createdTenantedUserIds)
    {
        _options = new UsageTranslationOptions(caller, forId, eventName, additional, createdTenantedUserIds);
    }

    public string UserId
    {
        get
        {
            if (_options.NotExists())
            {
                throw new InvalidOperationException(
                    Resources.UsageDeliveryTranslator_NotStarted.Format(nameof(StartTranslation)));
            }

            return _options.UserId;
        }
    }

    private sealed class UsageTranslationOptions
    {
        private const string AnonymousUserId = "anonymous";
        private const string UnTenantedValue = "platform";
        private const string UserIdDelimiter = "@";
        private readonly Dictionary<string, string>? _additional;
        private readonly bool _createdTenantedUserIds;
        private string _tenantId;
        private string? _tenantIdOverride;

        public UsageTranslationOptions(ICallerContext caller, string forId, string eventName,
            Dictionary<string, string>? additional, bool createdTenantedUserIds)
        {
            string? tenantIdOverride = null;
            switch (eventName)
            {
                case UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated:
                case UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged:
                    tenantIdOverride = additional.Exists()
                                       && additional.TryGetValue(UsageConstants.Properties.Id,
                                           out var organizationId)
                        ? organizationId
                        : null;
                    break;
            }

            _createdTenantedUserIds = createdTenantedUserIds;
            EventName = eventName;
            var tenantId = DetermineTenantId(caller.TenantId, tenantIdOverride, additional);
            _tenantId = tenantId;
            UserId = DetermineUserId(forId, tenantId, additional, createdTenantedUserIds);
            _tenantIdOverride = tenantIdOverride;
            _additional = additional;
        }

        public IReadOnlyDictionary<string, string>? Additional => _additional;

        public string EventName { get; }

        public string UserId { get; private set; }

        public string RecalculateTenantId(IReadOnlyDictionary<string, string>? additional)
        {
            if (additional.Exists())
            {
                if (_tenantIdOverride.HasValue())
                {
                    return _tenantIdOverride;
                }

                if (additional.TryGetValue(UsageConstants.Properties.TenantIdOverride, out var overriddenTenantId))
                {
                    return overriddenTenantId;
                }

                if (additional.TryGetValue(UsageConstants.Properties.TenantId, out var specifiedTenantId))
                {
                    return specifiedTenantId;
                }
            }

            return _tenantId;
        }

        public void RecalculateTenantId(string tenantId)
        {
            _tenantIdOverride = tenantId;
            _tenantId = DetermineTenantId(tenantId, _tenantIdOverride, _additional);
            UserId = DetermineUserId(UserId, _tenantId, _additional, _createdTenantedUserIds);
        }

        private static string DetermineUserId(string forId, string? tenantId, Dictionary<string, string>? additional,
            bool createdTenantedUserIds)
        {
            var userId = forId;
            if (additional.Exists())
            {
                if (additional.TryGetValue(UsageConstants.Properties.UserIdOverride, out var overriddenUserId))
                {
                    userId = overriddenUserId;
                }
            }

            if (userId.EqualsIgnoreCase(CallerConstants.AnonymousUserId))
            {
                userId = AnonymousUserId;
            }

            return createdTenantedUserIds
                ? $"{userId}{UserIdDelimiter}{tenantId}"
                : userId;
        }

        private static string DetermineTenantId(string? tenantId, string? tenantIdOverride,
            Dictionary<string, string>? additional)
        {
            if (additional.Exists())
            {
                if (tenantIdOverride.HasValue())
                {
                    return tenantIdOverride;
                }

                if (additional.TryGetValue(UsageConstants.Properties.TenantIdOverride, out var overriddenTenantId))
                {
                    return overriddenTenantId;
                }

                if (additional.TryGetValue(UsageConstants.Properties.TenantId, out var specifiedTenantId))
                {
                    return specifiedTenantId;
                }
            }

            return tenantId.HasValue()
                ? tenantId
                : UnTenantedValue;
        }
    }
}