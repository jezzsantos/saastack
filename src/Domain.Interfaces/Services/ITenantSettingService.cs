namespace Domain.Interfaces.Services;

/// <summary>
///     Defines a domain service for reading tenant settings
/// </summary>
public interface ITenantSettingService
{
    string Decrypt(string encryptedValue);
}