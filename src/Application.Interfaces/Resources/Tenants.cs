namespace Application.Interfaces.Resources;

/// <summary>
/// Defines a setting for a specific tenant
/// </summary>
public class TenantSetting
{
    public bool IsEncrypted { get; set; }

    public string? Value { get; set; }
}