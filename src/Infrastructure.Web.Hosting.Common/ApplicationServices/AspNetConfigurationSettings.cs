using Common;
using Common.Configuration;
using Common.Extensions;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Web.Hosting.Common.ApplicationServices;

/// <summary>
///     Provides settings read from .NET configuration
/// </summary>
public class AspNetConfigurationSettings : IConfigurationSettings
{
    private readonly Optional<ISettings> _tenantedSettings;

    public AspNetConfigurationSettings(IConfiguration configuration, ITenancyContext tenancy)
    {
        Platform = new AppSettingsWrapper(configuration);
        _tenantedSettings = new TenantedSettings(tenancy);
    }

    public AspNetConfigurationSettings(IConfiguration globalSettings)
    {
        Platform = new AppSettingsWrapper(globalSettings);
        _tenantedSettings = Optional<ISettings>.None;
    }

    internal AspNetConfigurationSettings(IConfiguration globalSettings, IConfiguration tenancySettings)
    {
        Platform = new AppSettingsWrapper(globalSettings);
        _tenantedSettings = new AppSettingsWrapper(tenancySettings);
    }

    public ISettings Platform { get; }

    public ISettings Tenancy
    {
        get
        {
            if (!_tenantedSettings.HasValue)
            {
                throw new NotImplementedException();
            }

            return _tenantedSettings.Value;
        }
    }

    private sealed class AppSettingsWrapper : ISettings
    {
        private readonly IConfiguration _configuration;

        public AppSettingsWrapper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool GetBool(string key)
        {
            var value = _configuration.GetValue<string>(key);
            if (value.HasNoValue())
            {
                throw new InvalidOperationException(Resources.AspNetConfigurationSettings_KeyNotFound.Format(key));
            }

            if (!bool.TryParse(value, out var boolean))
            {
                throw new InvalidOperationException(Resources.AspNetConfigurationSettings_ValueNotBoolean.Format(key));
            }

            return boolean;
        }

        public double GetNumber(string key)
        {
            var value = _configuration.GetValue<string>(key);
            if (value.HasNoValue())
            {
                throw new InvalidOperationException(Resources.AspNetConfigurationSettings_KeyNotFound.Format(key));
            }

            if (!double.TryParse(value, out var number))
            {
                throw new InvalidOperationException(Resources.AspNetConfigurationSettings_ValueNotNumber.Format(key));
            }

            return number;
        }

        public string GetString(string key)
        {
            var value = _configuration.GetValue<string>(key);
            if (value.NotExists())
            {
                throw new InvalidOperationException(Resources.AspNetConfigurationSettings_KeyNotFound.Format(key));
            }

            return value;
        }

        public bool IsConfigured => true;
    }

    private sealed class TenantedSettings : ISettings
    {
        private readonly ITenancyContext _tenancy;

        public TenantedSettings(ITenancyContext tenancy)
        {
            _tenancy = tenancy;
        }

        public bool GetBool(string key)
        {
            var settings = _tenancy.Settings;
            if (!settings.TryGetValue(key, out var value))
            {
                throw new InvalidOperationException(Resources.AspNetConfigurationSettings_KeyNotFound.Format(key));
            }

            if (!bool.TryParse(value.ToString(), out var boolean))
            {
                throw new InvalidOperationException(Resources.AspNetConfigurationSettings_ValueNotBoolean.Format(key));
            }

            return boolean;
        }

        public double GetNumber(string key)
        {
            var settings = _tenancy.Settings;
            if (!settings.TryGetValue(key, out var value))
            {
                throw new InvalidOperationException(Resources.AspNetConfigurationSettings_KeyNotFound.Format(key));
            }

            if (!double.TryParse(value.ToString(), out var number))
            {
                throw new InvalidOperationException(Resources.AspNetConfigurationSettings_ValueNotNumber.Format(key));
            }

            return number;
        }

        public string GetString(string key)
        {
            var settings = _tenancy.Settings;
            if (!settings.TryGetValue(key, out var value))
            {
                throw new InvalidOperationException(Resources.AspNetConfigurationSettings_KeyNotFound.Format(key));
            }

            return value.ToString() ?? string.Empty;
        }

        public bool IsConfigured => _tenancy.Current.HasValue();
    }
}