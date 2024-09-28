using Common;
using Common.Configuration;
using Common.Extensions;
using Infrastructure.Interfaces;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Hosting.Common;

/// <summary>
///     Provides settings dynamically from the current Tenancy first (if available),
///     then from Platform settings read from .NET configuration, and then a default value (if any given).
///     A <see cref="Tenancy" /> setting will only be used if all these conditions are satisfied:
///     1. The ctor that includes <see cref="ITenancyContext" /> is used,
///     and the instance is registered in the DI container as "scoped".
///     2. The <see cref="ITenancyContext" /> has a value for the <see cref="ITenancyContext.Current" />, which will only
///     be true is the current HTTP request is for a tenant.
///     3. The setting is defined in the <see cref="ITenancyContext.Settings" />
/// </summary>
public class AspNetDynamicConfigurationSettings : IConfigurationSettings
{
    private readonly ISettingsSafely _platform;
    private readonly Optional<ISettingsSafely> _tenantSettings;

    public AspNetDynamicConfigurationSettings(IConfiguration configuration, ITenancyContext tenancy)
    {
        _platform = new PlatformSettings(configuration);
        _tenantSettings = new TenantSettings(tenancy);
    }

    public AspNetDynamicConfigurationSettings(IConfiguration configuration)
    {
        _platform = new PlatformSettings(configuration);
        _tenantSettings = Optional<ISettingsSafely>.None;
    }

    private bool IsTenanted => _tenantSettings.HasValue;

    public bool GetBool(string key, bool? defaultValue = null)
    {
        if (IsTenanted)
        {
            var value = _tenantSettings.Value
                .GetBoolSafely(key); // Note: don't use the default here so we defer to the platform
            if (value.HasValue)
            {
                return value.Value;
            }

            value = _platform.GetBoolSafely(key, defaultValue);
            if (value.HasValue)
            {
                return value.Value;
            }

            throw new InvalidOperationException(Resources.AspNetDynamicConfigurationSettings_EitherSettings_KeyNotFound
                .Format(key));
        }

        return Platform.GetBool(key, defaultValue);
    }

    public double GetNumber(string key, double? defaultValue = null)
    {
        if (IsTenanted)
        {
            var value = _tenantSettings.Value
                .GetNumberSafely(key); // Note: don't use the default here so we defer to the platform
            if (value.HasValue)
            {
                return value.Value;
            }

            value = _platform.GetNumberSafely(key, defaultValue);
            if (value.HasValue)
            {
                return value.Value;
            }

            throw new InvalidOperationException(Resources.AspNetDynamicConfigurationSettings_EitherSettings_KeyNotFound
                .Format(key));
        }

        return Platform.GetNumber(key, defaultValue);
    }

    public string GetString(string key, string? defaultValue = null)
    {
        if (IsTenanted)
        {
            var value = _tenantSettings.Value
                .GetStringSafely(key); // Note: don't use the default here so we defer to the platform 
            if (value.HasValue)
            {
                return value;
            }

            value = _platform.GetStringSafely(key, defaultValue);
            if (value.HasValue)
            {
                return value.Value;
            }

            throw new InvalidOperationException(Resources.AspNetDynamicConfigurationSettings_EitherSettings_KeyNotFound
                .Format(key));
        }

        return Platform.GetString(key, defaultValue);
    }

    bool ISettings.IsConfigured => true;

    public ISettings Platform => _platform;

    public ISettings Tenancy
    {
        get
        {
            if (!IsTenanted)
            {
                throw new InvalidOperationException(Resources.AspNetDynamicConfigurationSettings_NoTenantSettings);
            }

            return _tenantSettings.Value;
        }
    }

    private interface ISettingsSafely : ISettings
    {
        Optional<bool> GetBoolSafely(string key, bool? defaultValue = null);

        Optional<double> GetNumberSafely(string key, double? defaultValue = null);

        Optional<string> GetStringSafely(string key, string? defaultValue = null);
    }

    private sealed class PlatformSettings : ISettingsSafely
    {
        private readonly IConfiguration _configuration;

        public PlatformSettings(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public bool GetBool(string key, bool? defaultValue = null)
        {
            var value = GetBoolSafely(key, defaultValue);
            if (value.HasValue)
            {
                return value.Value;
            }

            throw new InvalidOperationException(Resources
                .AspNetDynamicConfigurationSettings_PlatformSettings_KeyNotFound
                .Format(key));
        }

        public Optional<bool> GetBoolSafely(string key, bool? defaultValue = null)
        {
            var value = _configuration.GetValue<string>(key);
            if (value.HasNoValue())
            {
                if (defaultValue.HasValue)
                {
                    return defaultValue.Value;
                }

                return Optional<bool>.None;
            }

            if (!bool.TryParse(value, out var boolean))
            {
                throw new InvalidOperationException(
                    Resources.AspNetDynamicConfigurationSettings_ValueNotBoolean.Format(key));
            }

            return boolean;
        }

        public double GetNumber(string key, double? defaultValue = null)
        {
            var value = GetNumberSafely(key, defaultValue);
            if (value.HasValue)
            {
                return value.Value;
            }

            throw new InvalidOperationException(Resources
                .AspNetDynamicConfigurationSettings_PlatformSettings_KeyNotFound
                .Format(key));
        }

        public Optional<double> GetNumberSafely(string key, double? defaultValue = null)
        {
            var value = _configuration.GetValue<string>(key);
            if (value.HasNoValue())
            {
                if (defaultValue.HasValue)
                {
                    return defaultValue.Value;
                }

                return Optional<double>.None;
            }

            if (!double.TryParse(value, out var number))
            {
                throw new InvalidOperationException(
                    Resources.AspNetDynamicConfigurationSettings_ValueNotNumber.Format(key));
            }

            return number;
        }

        public string GetString(string key, string? defaultValue = null)
        {
            var value = GetStringSafely(key, defaultValue);
            if (value.HasValue)
            {
                return value.Value;
            }

            throw new InvalidOperationException(Resources
                .AspNetDynamicConfigurationSettings_PlatformSettings_KeyNotFound
                .Format(key));
        }

        public Optional<string> GetStringSafely(string key, string? defaultValue = null)
        {
            var value = _configuration.GetValue<string>(key);
            if (value.NotExists())
            {
                if (defaultValue.Exists())
                {
                    return defaultValue;
                }

                return Optional<string>.None;
            }

            return value;
        }

        public bool IsConfigured => true;
    }

    private sealed class TenantSettings : ISettingsSafely
    {
        private readonly ITenancyContext _tenancy;

        public TenantSettings(ITenancyContext tenancy)
        {
            _tenancy = tenancy;
        }

        public bool GetBool(string key, bool? defaultValue = null)
        {
            var value = GetBoolSafely(key, defaultValue);
            if (value.HasValue)
            {
                return value.Value;
            }

            throw new InvalidOperationException(Resources.AspNetDynamicConfigurationSettings_TenantSettings_KeyNotFound
                .Format(key));
        }

        public Optional<bool> GetBoolSafely(string key, bool? defaultValue = null)
        {
            var settings = _tenancy.Settings;
            if (!settings.TryGetValue(key, out var value))
            {
                if (defaultValue.HasValue)
                {
                    return defaultValue.Value;
                }

                return Optional<bool>.None;
            }

            if (value.Value.NotExists())
            {
                if (defaultValue.HasValue)
                {
                    return defaultValue.Value;
                }

                return Optional<bool>.None;
            }

            if (!bool.TryParse(value.Value.ToString(), out var boolean))
            {
                throw new InvalidOperationException(
                    Resources.AspNetDynamicConfigurationSettings_ValueNotBoolean.Format(key));
            }

            return boolean;
        }

        public double GetNumber(string key, double? defaultValue = null)
        {
            var value = GetNumberSafely(key, defaultValue);
            if (value.HasValue)
            {
                return value.Value;
            }

            throw new InvalidOperationException(Resources.AspNetDynamicConfigurationSettings_TenantSettings_KeyNotFound
                .Format(key));
        }

        public Optional<double> GetNumberSafely(string key, double? defaultValue = null)
        {
            var settings = _tenancy.Settings;
            if (!settings.TryGetValue(key, out var value))
            {
                if (defaultValue.HasValue)
                {
                    return defaultValue.Value;
                }

                return Optional<double>.None;
            }

            if (value.Value.NotExists())
            {
                if (defaultValue.HasValue)
                {
                    return defaultValue.Value;
                }

                return Optional<double>.None;
            }

            if (!double.TryParse(value.Value.ToString(), out var number))
            {
                throw new InvalidOperationException(
                    Resources.AspNetDynamicConfigurationSettings_ValueNotNumber.Format(key));
            }

            return number;
        }

        public string GetString(string key, string? defaultValue = null)
        {
            var value = GetStringSafely(key, defaultValue);
            if (value.HasValue)
            {
                return value.Value;
            }

            throw new InvalidOperationException(Resources.AspNetDynamicConfigurationSettings_TenantSettings_KeyNotFound
                .Format(key));
        }

        public Optional<string> GetStringSafely(string key, string? defaultValue = null)
        {
            var settings = _tenancy.Settings;
            if (!settings.TryGetValue(key, out var value))
            {
                if (defaultValue.Exists())
                {
                    return defaultValue;
                }

                return Optional<string>.None;
            }

            if (value.Value.NotExists())
            {
                if (defaultValue.Exists())
                {
                    return defaultValue;
                }

                return Optional<string>.None;
            }

            return value.Value.ToString() ?? string.Empty;
        }

        public bool IsConfigured => _tenancy.Current.HasValue();
    }
}