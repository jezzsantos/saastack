using System.Collections.ObjectModel;

namespace Application.Interfaces.Services;

/// <summary>
///     Defines a collection of <see cref="TenantSetting" />
/// </summary>
public sealed class TenantSettings : ReadOnlyDictionary<string, TenantSetting>
{
    public TenantSettings() : this(new Dictionary<string, TenantSetting>())
    {
    }

    public TenantSettings(IDictionary<string, TenantSetting> dictionary) : base(dictionary)
    {
    }

    public TenantSettings(IDictionary<string, object> dictionary) : base(dictionary.ToDictionary(pair => pair.Key,
        pair => new TenantSetting
        {
            Value = pair.Value
        }))
    {
    }
}

/// <summary>
///     Defines a setting for a specific tenant
/// </summary>
public sealed class TenantSetting
{
    public TenantSetting()
    {
        IsEncrypted = false;
        Value = null;
    }

    public TenantSetting(object? value, bool isEncrypted = false)
    {
        IsEncrypted = isEncrypted;
        Value = value;
    }

    public bool IsEncrypted { get; set; }

    public object? Value { get; set; }
}