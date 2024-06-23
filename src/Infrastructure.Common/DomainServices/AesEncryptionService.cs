using System.Security.Cryptography;
using Domain.Services.Shared;

namespace Infrastructure.Common.DomainServices;

/// <summary>
///     Provides a domain service for encrypting values, using AES encryption
/// </summary>
public class AesEncryptionService : IEncryptionService
{
    private const string SecretKeyDelimiter = "::";
    private readonly string _aesSecret;

    public AesEncryptionService(string aesSecret)
    {
        _aesSecret = aesSecret;
    }

    public string Decrypt(string cipherText)
    {
        using var aes = CreateAes();
        var (key, iv) = GetAesKeysFromSecret(_aesSecret);
        using var decryptor = aes.CreateDecryptor(key, iv);
        var cipher = Convert.FromBase64String(cipherText);
        using var ms = new MemoryStream(cipher);
        using var cryptoStream = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using var reader = new StreamReader(cryptoStream);
        return reader.ReadToEnd();
    }

    public string Encrypt(string plainText)
    {
        using var aes = CreateAes();
        var (key, iv) = GetAesKeysFromSecret(_aesSecret);
        using var encryptor = aes.CreateEncryptor(key, iv);

        byte[] cipher;
        using (var ms = new MemoryStream())
        {
            using (var cryptoStream = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                using (var writer = new StreamWriter(cryptoStream))
                {
                    writer.Write(plainText);
                }

                cipher = ms.ToArray();
            }
        }

        return Convert.ToBase64String(cipher);
    }

    private static (byte[] key, byte[] iv) GetAesKeysFromSecret(string aesSecret)
    {
        var rightSide = aesSecret.Substring(0, aesSecret.IndexOf(SecretKeyDelimiter, StringComparison.Ordinal));
        var leftSide = aesSecret.Substring(aesSecret.IndexOf(SecretKeyDelimiter, StringComparison.Ordinal)
                                           + SecretKeyDelimiter.Length);
        var key = Convert.FromBase64String(rightSide);
        var iv = Convert.FromBase64String(leftSide);

        return (key, iv);
    }

    private static SymmetricAlgorithm CreateAes()
    {
        var aes = Aes.Create();
        aes.KeySize = 256;
        aes.BlockSize = 128;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        return aes;
    }

#if TESTINGONLY
    public static string CreateAesSecret()
    {
        CreateKeyAndIv(out var key, out var iv);
        return $"{Convert.ToBase64String(key)}{SecretKeyDelimiter}{Convert.ToBase64String(iv)}";
    }

    private static void CreateKeyAndIv(out byte[] key, out byte[] iv)
    {
        using var aes = CreateAes();
        key = aes.Key;
        iv = aes.IV;
    }
#endif
}