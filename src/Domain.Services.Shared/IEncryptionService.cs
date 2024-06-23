namespace Domain.Services.Shared;

public interface IEncryptionService
{
    string Decrypt(string encryptedValue);

    string Encrypt(string plainText);
}