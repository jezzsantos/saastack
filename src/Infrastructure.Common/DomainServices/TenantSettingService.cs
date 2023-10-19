using Domain.Interfaces.Services;

namespace Infrastructure.Common.DomainServices;

/// <summary>
///     Provides a domain service for handling settings for a tenant
/// </summary>
public class TenantSettingService : ITenantSettingService
{
    public const string EncryptionServiceSecretSettingName = "DomainServices:TenantSettingService:AesSecret";

    private readonly AesEncryptionService _encryptionService;

    public TenantSettingService(AesEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public string Decrypt(string encryptedValue)
    {
        return _encryptionService.Decrypt(encryptedValue);
    }
}