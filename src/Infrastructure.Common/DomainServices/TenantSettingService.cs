using Domain.Interfaces.Services;
using Domain.Services.Shared;

namespace Infrastructure.Common.DomainServices;

/// <summary>
///     Provides a domain service for handling settings for a tenant
/// </summary>
public class TenantSettingService : ITenantSettingService
{
    public const string EncryptionServiceSecretSettingName = "DomainServices:TenantSettingService:AesSecret";

    private readonly IEncryptionService _encryptionService;

    public TenantSettingService(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public string Decrypt(string encryptedValue)
    {
        return _encryptionService.Decrypt(encryptedValue);
    }

    public string Encrypt(string plainText)
    {
        return _encryptionService.Encrypt(plainText);
    }
}