using System.Security.Cryptography;
using Domain.Services.Shared.DomainServices;

namespace Infrastructure.Shared.ApplicationServices;

public sealed class TokensService : ITokensService
{
    private const int DefaultTokenSizeInBytes = 32;

    public string CreateTokenForPasswordReset()
    {
        return GenerateRandomToken();
    }

    public string CreateTokenForJwtRefresh()
    {
        return GenerateRandomToken();
    }

    public string CreateTokenForVerification()
    {
        return GenerateRandomToken();
    }

    private static string GenerateRandomToken(int keySize = DefaultTokenSizeInBytes)
    {
        using (var random = RandomNumberGenerator.Create())
        {
            var bytes = new byte[keySize];
            random.GetNonZeroBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}