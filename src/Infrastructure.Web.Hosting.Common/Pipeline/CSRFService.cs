using Application.Interfaces.Services;
using Common;
using Domain.Services.Shared;

namespace Infrastructure.Web.Hosting.Common.Pipeline;

/// <summary>
///     Provides a service for creating and verifying CSRF tokens
/// </summary>
public class CSRFService : CSRFMiddleware.ICSRFService
{
    private readonly IEncryptionService _encryptionService;
    private readonly string _hmacSecret;

    public CSRFService(IHostSettings settings, IEncryptionService encryptionService)
    {
        _hmacSecret = settings.GetWebsiteHostCSRFSigningSecret();
        _encryptionService = encryptionService;
    }

    public CSRFTokenPair CreateTokens(Optional<string> userId)
    {
        return CSRFTokenPair.CreateTokens(_encryptionService, _hmacSecret, userId);
    }

    public bool VerifyTokens(Optional<string> token, Optional<string> signature, Optional<string> userId)
    {
        if (!token.HasValue)
        {
            return false;
        }

        if (!signature.HasValue)
        {
            return false;
        }

        return CSRFTokenPair.FromTokens(token, signature)
            .IsValid(_encryptionService, _hmacSecret, userId);
    }
}