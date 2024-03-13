namespace Domain.Services.Shared.DomainServices;

public interface IEncryptionService
{
    string Decrypt(string encryptedValue);

    string Encrypt(string plainText);
}