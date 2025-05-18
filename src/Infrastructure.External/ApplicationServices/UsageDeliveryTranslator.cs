using Application.Interfaces;
using Common.Extensions;
using Domain.Interfaces;
using UAParser;

namespace Infrastructure.External.ApplicationServices;

/// <summary>
///     Provides a basic implementation of the <see cref="IUsageDeliveryTranslator" /> interface.
/// </summary>
public class UsageDeliveryTranslator : IUsageDeliveryTranslator
{
    public const string BrowserIp = "Ip";
    public const string BrowserReferrer = "Referrer";
    private static readonly string[] IgnoredCustomEventProperties =
    [
        UsageConstants.Properties.UserIdOverride,
        UsageConstants.Properties.TenantIdOverride,
        UsageConstants.Properties.DefaultOrganizationId
    ];
    private UsageTranslationOptions? _options;

    public string TenantId
    {
        get
        {
            if (_options.NotExists())
            {
                throw new InvalidOperationException(
                    Resources.UsageDeliveryTranslator_NotStarted.Format(nameof(StartTranslation)));
            }

            return _options.TenantId;
        }
    }

    public BrowserComponents GetBrowserProperties(Dictionary<string, string> additional)
    {
        var properties = new BrowserComponents
        {
            Referrer = additional.GetValueOrDefault(BrowserReferrer)
                       ?? additional.GetValueOrDefault(UsageConstants.Properties.ReferredBy),
            Url = additional.GetValueOrDefault(UsageConstants.Properties.Path),
            IpAddress = additional.GetValueOrDefault(BrowserIp)
                        ?? additional.GetValueOrDefault(UsageConstants.Properties.IpAddress)
        };
        return properties;
    }

    public UserAgentComponents GetUserAgentProperties(string? userAgent)
    {
        if (userAgent.HasNoValue())
        {
            return new UserAgentComponents();
        }

        var parser = Parser.GetDefault();
        var info = parser.Parse(userAgent);

        var browser = info.UA.Family;
        var browserVersion = $"{info.UA.Major}.{info.UA.Minor}.{info.UA.Patch}";
        var os = info.OS.Family;
        var device = info.Device.Family;
        if (device.EqualsIgnoreCase("other"))
        {
            device = null;
        }

        return new UserAgentComponents
        {
            Browser = browser,
            BrowserVersion = browserVersion,
            OperatingSystem = os,
            Device = device
        };
    }

    public UserComponents GetUserProperties(string eventName, Dictionary<string, string>? additional)
    {
        var components = new UserComponents();
        if (eventName is UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated
            or UsageConstants.Events.UsageScenarios.Generic.MachineRegistered
            or UsageConstants.Events.UsageScenarios.Generic.MembershipAdded)
        {
            components.CreatedAt = DateTime.UtcNow.ToNearestSecond();
        }

        if (additional.NotExists()
            || additional.HasNone())
        {
            return components;
        }

        if (eventName
            is UsageConstants.Events.UsageScenarios.Generic.UserLogin
            or UsageConstants.Events.UsageScenarios.Generic.PersonRegistrationCreated
            or UsageConstants.Events.UsageScenarios.Generic.MachineRegistered
            or UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged
            or UsageConstants.Events.UsageScenarios.Generic.MembershipChanged
           )
        {
            if (additional.TryGetValue(UsageConstants.Properties.Name, out var name))
            {
                components.Name = name;
            }

            if (additional.TryGetValue(UsageConstants.Properties.EmailAddress, out var emailAddress))
            {
                components.EmailAddress = emailAddress;
            }
        }

        if (additional.TryGetValue(UsageConstants.Properties.Timezone, out var timezone))
        {
            components.Timezone = timezone;
        }

        if (additional.TryGetValue(UsageConstants.Properties.CountryCode, out var countryCode))
        {
            components.CountryCode = countryCode;
        }

        if (additional.TryGetValue(UsageConstants.Properties.AvatarUrl, out var avatarUrl))
        {
            components.AvatarUrl = avatarUrl;
        }

        return components;
    }

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

        if (_options.EventName
            is UsageConstants.Events.UsageScenarios.Generic.UserLogin)
        {
            return _options.Additional.TryGetValue(UsageConstants.Properties.UserIdOverride, out _);
        }

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

        if (_options.EventName
            is UsageConstants.Events.UsageScenarios.Generic.OrganizationCreated
            or UsageConstants.Events.UsageScenarios.Generic.OrganizationChanged)
        {
            return _options.Additional.TryGetValue(UsageConstants.Properties.Id, out _);
        }

        if (_options.EventName
            is UsageConstants.Events.UsageScenarios.Generic.MembershipAdded
            or UsageConstants.Events.UsageScenarios.Generic.MembershipChanged)
        {
            return _options.Additional.TryGetValue(UsageConstants.Properties.Id, out _)
                   && _options.Additional.TryGetValue(UsageConstants.Properties.TenantIdOverride, out _);
        }

        return false;
    }

    public Dictionary<string, string> PrepareProperties(bool overrideOrganizationId, Func<string, string> converter)
    {
        if (_options.NotExists())
        {
            throw new InvalidOperationException(
                Resources.UsageDeliveryTranslator_NotStarted.Format(nameof(StartTranslation)));
        }

        var properties = new Dictionary<string, string>();
        var tenantId = _options.RecalculateTenantId(_options.Additional);
        if (overrideOrganizationId)
        {
            if (_options.EventName
                is UsageConstants.Events.UsageScenarios.Generic.UserLogin
                or UsageConstants.Events.UsageScenarios.Generic.UserProfileChanged)
            {
                if (_options.Additional.Exists())
                {
                    if (_options.Additional.TryGetValue(UsageConstants.Properties.DefaultOrganizationId,
                            out var defaultOrganizationId))
                    {
                        _options.RecalculateTenantId(defaultOrganizationId);
                        tenantId = defaultOrganizationId;
                    }
                }
            }
        }

        properties.TryAdd(UsageConstants.Properties.TenantId, tenantId);

        if (_options.Additional.NotExists()
            || _options.Additional.HasNone())
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
            TenantId = tenantId;
            UserId = DetermineUserId(forId, tenantId, additional, createdTenantedUserIds);
            _tenantIdOverride = tenantIdOverride;
            _additional = additional;
        }

        public IReadOnlyDictionary<string, string>? Additional => _additional;

        public string EventName { get; }

        public string TenantId { get; private set; }

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

            return TenantId;
        }

        public void RecalculateTenantId(string tenantId)
        {
            _tenantIdOverride = tenantId;
            TenantId = DetermineTenantId(tenantId, _tenantIdOverride, _additional);
            UserId = DetermineUserId(UserId, TenantId, _additional, _createdTenantedUserIds);
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