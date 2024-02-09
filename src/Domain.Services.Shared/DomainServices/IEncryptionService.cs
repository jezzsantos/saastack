namespace Domain.Services.Shared.DomainServices;

public interface IEncryptionService
{
    string Encrypt(string plainText);

    string Decrypt(string encryptedValue);
}